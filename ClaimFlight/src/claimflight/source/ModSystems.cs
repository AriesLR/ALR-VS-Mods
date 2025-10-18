using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claimflight
{
    [ProtoContract]
    public class FlightStatusMessage
    {
        [ProtoMember(1)]
        public bool IsFlying { get; set; }

        public FlightStatusMessage()
        { }
    }

    [ProtoContract]
    public class FlightToggleRequestMessage
    {
        public FlightToggleRequestMessage()
        { }
    }

    public sealed class ClaimFlightModSystem : ModSystem
    {
        private ICoreServerAPI? sapi;
        private ICoreClientAPI? capi;
        private const int TickMs = 1000;

        private readonly Dictionary<string, bool> playerFlightState = new();
        private readonly Dictionary<string, float> originalFallDamage = new();
        private readonly Dictionary<string, DateTime> lastInsideClaim = new();
        private readonly Dictionary<string, System.Timers.Timer> leaveClaimTimers = new();

        private DateTime lastToggleTime = DateTime.MinValue;
        private const int ToggleCooldownSeconds = 1;

        private bool clientIsFlying = false;
        private bool clientAwaitingAck = false;
        private float clientOriginalFallDamage = 1f;
        private double clientResyncTimer = 0;
        private double lastFlightEndTime = 0;

        private const int FlightDisableDelaySeconds = 30;
        private const int LeaveClaimGraceSeconds = 5;
        private const int ClientSyncIntervalSeconds = 3;

        private const double LocalFlightGraceSeconds = 0.15;
        private double lastResyncSendTime = 0;
        private const double ResyncDebounceSeconds = 0.25;

        private const double PostFlightSafeSeconds = 5.0;

        public override void Start(ICoreAPI api)
        {
            var channel = api.Network.RegisterChannel("claimflight");
            channel.RegisterMessageType<FlightStatusMessage>();
            channel.RegisterMessageType<FlightToggleRequestMessage>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api ?? throw new ArgumentNullException(nameof(api));
            var channel = sapi.Network.GetChannel("claimflight");

            channel.SetMessageHandler<FlightToggleRequestMessage>((IServerPlayer player, FlightToggleRequestMessage msg) =>
            {
                if (player?.Entity == null) return;

                var pos = player.Entity.ServerPos.AsBlockPos;
                ILandClaimAPI claimApi = sapi!.World.Claims;
                bool allowed = false;

                try
                {
                    var claims = claimApi.Get(pos);
                    allowed = claims != null && claims.Length > 0 && claimApi.TryAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak);
                }
                catch { }

                bool isFlying = playerFlightState.TryGetValue(player.PlayerUID, out var flying) && flying;

                if (!allowed)
                {
                    DisableFlightServerSide(player, channel, sendMessage: true, customMessageKey: "claimflight:deny_flight");
                    return;
                }

                if (isFlying)
                    DisableFlightServerSide(player, channel, sendMessage: true);
                else
                    EnableFlightServerSide(player, channel, sendMessage: true);
            });

            channel.SetMessageHandler<FlightStatusMessage>((IServerPlayer player, FlightStatusMessage msg) =>
            {
                playerFlightState[player.PlayerUID] = msg.IsFlying;
            });

            sapi.Event.RegisterGameTickListener(OnServerTick, TickMs);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api ?? throw new ArgumentNullException(nameof(api));
            var ch = capi.Network.GetChannel("claimflight");

            capi.Event.PlayerJoin += OnClientPlayerJoin;
            capi.Event.RegisterGameTickListener(OnClientTick, 0);

            capi.Input.RegisterHotKey("toggleclaimflight", Lang.Get("claimflight:toggle_hotkey") ?? "Toggle Flight", GlKeys.Up, HotkeyType.CharacterControls);
            capi.Input.SetHotKeyHandler("toggleclaimflight", (comb) =>
            {
                try
                {
                    if ((DateTime.UtcNow - lastToggleTime).TotalSeconds >= ToggleCooldownSeconds)
                    {
                        ch.SendPacket(new FlightToggleRequestMessage());
                        lastToggleTime = DateTime.UtcNow;
                        clientAwaitingAck = true;
                    }
                }
                catch { }

                return true;
            });

            ch.SetMessageHandler<FlightStatusMessage>(msg =>
            {
                var player = capi?.World?.Player;
                if (player == null) return;
                var entity = player.Entity;
                var worldData = player.WorldData;
                if (entity == null || worldData == null) return;

                if (msg.IsFlying == clientIsFlying)
                {
                    clientAwaitingAck = false;
                    return;
                }

                clientIsFlying = msg.IsFlying;
                clientAwaitingAck = false;

                try
                {
                    if (msg.IsFlying)
                    {
                        clientOriginalFallDamage = entity.Properties.FallDamageMultiplier;
                        worldData.FreeMove = true;
                        worldData.NoClip = false;
                        entity.Properties.FallDamageMultiplier = 0f;
                        ResetFallState(entity);
                    }
                    else
                    {
                        worldData.FreeMove = false;
                        worldData.NoClip = false;
                        entity.Properties.FallDamageMultiplier = clientOriginalFallDamage;
                        ResetFallState(entity);

                        lastFlightEndTime = (capi?.World?.ElapsedMilliseconds ?? 0) / 1000.0;
                    }
                }
                catch { }
            });
        }

        private void OnClientPlayerJoin(IClientPlayer player)
        {
            if (player?.Entity?.Properties != null)
                clientOriginalFallDamage = player.Entity.Properties.FallDamageMultiplier;

            var worldData = player?.WorldData;
            if (worldData != null)
            {
                worldData.FreeMove = false;
                worldData.NoClip = false;
            }

            if (player?.Entity != null)
            {
                ResetFallState(player.Entity);
                player.Entity.Properties.FallDamageMultiplier = 1f;
            }

            clientIsFlying = false;
            clientAwaitingAck = false;
        }

        private void ResetFallState(EntityPlayer? entity)
        {
            if (entity == null) return;
            entity.Properties.FallDamageMultiplier = 0f;
            entity.Attributes.SetFloat("fallDistance", 0f);
            var motion = entity.Pos?.Motion;
            if (motion != null && motion.Y < -0.1f)
                motion.Y = 0;
        }

        private void OnClientTick(float dt)
        {
            if (capi == null) return;
            clientResyncTimer += dt;

            var player = capi.World?.Player;
            if (player == null) return;
            var worldData = player.WorldData;
            if (worldData == null) return;

            var ch = capi.Network.GetChannel("claimflight");
            double nowSeconds = (capi.World?.ElapsedMilliseconds ?? 0) / 1000.0;
            var entity = player.Entity;
            if (entity == null) return;

            bool withinFlightGrace = nowSeconds - lastFlightEndTime < LocalFlightGraceSeconds;
            bool withinSafeWindow = nowSeconds - lastFlightEndTime < PostFlightSafeSeconds;

            // Disable fall damage and reset fall distance while flying or within post-flight safe window
            if (clientIsFlying || withinSafeWindow)
            {
                entity.Attributes.SetFloat("fallDistance", 0f);
                entity.Properties.FallDamageMultiplier = 0f;
            }
            else
            {
                // Restore normal fall damage multiplier after safety expires
                entity.Properties.FallDamageMultiplier = clientOriginalFallDamage;
            }

            // Restore when server toggles flight off but client still in flying state
            if (clientIsFlying && !worldData.FreeMove && !withinFlightGrace)
            {
                worldData.FreeMove = true;
                worldData.NoClip = false;
                entity.Properties.FallDamageMultiplier = 0f;
                ResetFallState(entity);
                clientResyncTimer = 0;

                if (nowSeconds - lastResyncSendTime >= ResyncDebounceSeconds)
                {
                    ch.SendPacket(new FlightStatusMessage { IsFlying = true });
                    lastResyncSendTime = nowSeconds;
                }
            }

            // Resync mismatched states
            if (!clientAwaitingAck && worldData.FreeMove != clientIsFlying)
            {
                if (nowSeconds - lastResyncSendTime >= ResyncDebounceSeconds)
                {
                    ch.SendPacket(new FlightStatusMessage { IsFlying = clientIsFlying });
                    lastResyncSendTime = nowSeconds;
                }
            }

            // Periodic sync
            if (clientResyncTimer >= ClientSyncIntervalSeconds)
            {
                clientResyncTimer = 0;
                ch.SendPacket(new FlightStatusMessage { IsFlying = clientIsFlying });
                lastResyncSendTime = nowSeconds;
            }
        }

        private void OnServerTick(float dt)
        {
            if (sapi == null) return;
            var channel = sapi.Network.GetChannel("claimflight");
            var claimApi = sapi.World.Claims;

            foreach (var player in sapi.World.AllOnlinePlayers)
            {
                if (player?.Entity == null) continue;
                if (player is not IServerPlayer splayer) continue;

                var pos = splayer.Entity.ServerPos.AsBlockPos;
                bool allowed = false;

                try
                {
                    var claims = claimApi.Get(pos);
                    allowed = claims != null && claims.Length > 0 && claimApi.TryAccess(splayer, pos, EnumBlockAccessFlags.BuildOrBreak);
                }
                catch { }

                bool isFlying = playerFlightState.TryGetValue(splayer.PlayerUID, out var flying) && flying;

                if (!allowed && isFlying)
                {
                    if (lastInsideClaim.TryGetValue(splayer.PlayerUID, out var lastInside))
                    {
                        var secondsOutside = (DateTime.UtcNow - lastInside).TotalSeconds;
                        if (secondsOutside < LeaveClaimGraceSeconds) continue;
                    }

                    if (!leaveClaimTimers.ContainsKey(splayer.PlayerUID))
                    {
                        var timer = new System.Timers.Timer(1000);
                        int secondsLeft = FlightDisableDelaySeconds;

                        timer.Elapsed += (sender, e) =>
                        {
                            if (!playerFlightState.TryGetValue(splayer.PlayerUID, out var flying2) || !flying2)
                            {
                                timer.Stop();
                                leaveClaimTimers.Remove(splayer.PlayerUID);
                                return;
                            }

                            if (secondsLeft == 30 || secondsLeft == 15 || secondsLeft == 5)
                            {
                                int displaySeconds = secondsLeft;
                                sapi!.Event.EnqueueMainThreadTask(() =>
                                {
                                    splayer.SendMessage(GlobalConstants.InfoLogChatGroup,
                                        Lang.Get("claimflight:leave_claim_timer", displaySeconds) ?? $"Flight will disable in {displaySeconds} seconds",
                                        EnumChatType.Notification);
                                }, "claimflight_warn");
                            }

                            secondsLeft--;

                            if (secondsLeft <= 0)
                            {
                                sapi!.Event.EnqueueMainThreadTask(() =>
                                {
                                    DisableFlightServerSide(splayer, channel, sendMessage: true,
                                        customMessageKey: "claimflight:flight_disabled_outside_claim");
                                    timer.Stop();
                                    leaveClaimTimers.Remove(splayer.PlayerUID);
                                }, "claimflight_disable");
                            }
                        };

                        leaveClaimTimers[splayer.PlayerUID] = timer;
                        timer.Start();
                    }
                }
                else if (allowed)
                {
                    lastInsideClaim[splayer.PlayerUID] = DateTime.UtcNow;
                    if (leaveClaimTimers.TryGetValue(splayer.PlayerUID, out var existingTimer))
                    {
                        existingTimer.Stop();
                        leaveClaimTimers.Remove(splayer.PlayerUID);
                    }
                }
            }
        }

        private void EnableFlightServerSide(IServerPlayer player, IServerNetworkChannel channel, bool sendMessage = false, string? customMessageKey = null)
        {
            if (player.Entity is not EntityPlayer entityPlayer) return;

            if (!originalFallDamage.ContainsKey(player.PlayerUID))
                originalFallDamage[player.PlayerUID] = entityPlayer.Properties.FallDamageMultiplier;

            player.WorldData.FreeMove = true;
            player.WorldData.NoClip = false;
            playerFlightState[player.PlayerUID] = true;
            lastInsideClaim[player.PlayerUID] = DateTime.UtcNow;

            channel.SendPacket(new FlightStatusMessage { IsFlying = true }, player);

            if (sendMessage)
            {
                string messageKey = customMessageKey ?? "claimflight:flight_enabled";
                player.SendMessage(GlobalConstants.InfoLogChatGroup,
                    Lang.Get(messageKey) ?? "Flight enabled!",
                    EnumChatType.Notification);
            }
        }

        private void DisableFlightServerSide(IServerPlayer player, IServerNetworkChannel channel, bool sendMessage = false, string? customMessageKey = null)
        {
            if (player.Entity is not EntityPlayer entityPlayer) return;

            // Store original fall damage, this should be 1f all the time, but I already did it this way so I'm leaving it.
            float originalFD = originalFallDamage.TryGetValue(player.PlayerUID, out var val) ? val : 1f;

            entityPlayer.Properties.FallDamageMultiplier = 0f;

            player.WorldData.FreeMove = false;
            player.WorldData.NoClip = false;
            playerFlightState[player.PlayerUID] = false;

            channel.SendPacket(new FlightStatusMessage { IsFlying = false }, player);

            // Use a timer to restore fall damage after PostFlightSafeSeconds
            var restoreTimer = new System.Timers.Timer(PostFlightSafeSeconds * 1000);
            restoreTimer.AutoReset = false;
            restoreTimer.Elapsed += (s, e) =>
            {
                restoreTimer.Stop();
                restoreTimer.Dispose();
                sapi?.Event.EnqueueMainThreadTask(() =>
                {
                    entityPlayer.Properties.FallDamageMultiplier = originalFD;
                }, "restoreFallDamage");
            };
            restoreTimer.Start();

            if (sendMessage)
            {
                string messageKey = customMessageKey ?? "claimflight:flight_disabled";
                player.SendMessage(GlobalConstants.InfoLogChatGroup,
                    Lang.Get(messageKey) ?? "Flight disabled!",
                    EnumChatType.Notification);
            }
        }
    }
}