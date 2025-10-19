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

        private readonly Dictionary<string, bool> playerFlightState = new();
        private readonly Dictionary<string, DateTime> lastInsideClaim = new();
        private readonly Dictionary<string, System.Timers.Timer> leaveClaimTimers = new();

        private DateTime lastToggleTime = DateTime.MinValue;
        private const int ToggleCooldownSeconds = 1;

        private bool clientIsFlying = false;
        private bool clientAwaitingAck = false;
        private double clientResyncTimer = 0;
        private double lastFlightEndTime = 0;

        private const int FlightDisableDelaySeconds = 30;
        private const int LeaveClaimGraceSeconds = 5;
        private const int ClientSyncIntervalSeconds = 3;
        private const double LocalFlightGraceSeconds = 0.15;
        private const double PostFlightSafeSeconds = 5.0;

        private long? clientTickListenerId;
        private long? serverTickListenerId;

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
                    DisableFlightServerSide(player, channel, sendMessage: true, "claimflight:deny_flight");
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
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api ?? throw new ArgumentNullException(nameof(api));
            var ch = capi.Network.GetChannel("claimflight");

            capi.Event.PlayerJoin += OnClientPlayerJoin;

            capi.Input.RegisterHotKey("toggleclaimflight", Lang.Get("claimflight:toggle_hotkey") ?? "Toggle Flight", GlKeys.Up, HotkeyType.CharacterControls);
            capi.Input.SetHotKeyHandler("toggleclaimflight", (comb) =>
            {
                try
                {
                    if (clientAwaitingAck) return true;
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
                        capi?.Logger.Notification("[ClaimFlight] Client flight enabled, registering tick listener.");
                        RegisterClientTick();
                        worldData.FreeMove = true;
                        worldData.NoClip = false;
                        entity.Properties.FallDamageMultiplier = 0f;
                        ResetFallState(entity);
                    }
                    else
                    {
                        capi?.Logger.Notification("[ClaimFlight] Client flight disabled, unregistering tick listener.");
                        UnregisterClientTick();
                        worldData.FreeMove = false;
                        worldData.NoClip = false;
                        entity.Properties.FallDamageMultiplier = 1f;
                        ResetFallState(entity);
                        lastFlightEndTime = (capi?.World?.ElapsedMilliseconds ?? 0) / 1000.0;
                    }
                }
                catch { }
            });
        }

        private void RegisterClientTick()
        {
            if (capi == null || clientTickListenerId != null) return;
            clientTickListenerId = capi.Event.RegisterGameTickListener(OnClientTick, 0);
            capi.Logger.Notification("[ClaimFlight] Client tick listener registered.");
        }

        private void UnregisterClientTick()
        {
            if (capi == null || clientTickListenerId == null) return;
            capi.Event.UnregisterGameTickListener(clientTickListenerId.Value);
            clientTickListenerId = null;
            capi.Logger.Notification("[ClaimFlight] Client tick listener unregistered.");
        }

        private void RegisterServerTick()
        {
            if (sapi == null || serverTickListenerId != null) return;
            serverTickListenerId = sapi.Event.RegisterGameTickListener(OnServerTick, 1000);
            sapi.Logger.Notification("[ClaimFlight] Server tick listener registered.");
        }

        private void UnregisterServerTick()
        {
            if (sapi == null || serverTickListenerId == null) return;
            sapi.Event.UnregisterGameTickListener(serverTickListenerId.Value);
            serverTickListenerId = null;
            sapi.Logger.Notification("[ClaimFlight] Server tick listener unregistered.");
        }

        private void OnClientPlayerJoin(IClientPlayer player)
        {
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
            var entity = player.Entity;
            if (entity == null) return;

            double nowSeconds = (capi.World?.ElapsedMilliseconds ?? 0) / 1000.0;
            bool withinSafeWindow = nowSeconds - lastFlightEndTime < PostFlightSafeSeconds;

            if (clientIsFlying || withinSafeWindow)
            {
                entity.Attributes.SetFloat("fallDistance", 0f);
                entity.Properties.FallDamageMultiplier = 0f;
            }
            else
            {
                entity.Properties.FallDamageMultiplier = 1f;
            }

            if (clientResyncTimer >= ClientSyncIntervalSeconds)
            {
                clientResyncTimer = 0;
                var ch = capi.Network.GetChannel("claimflight");
                ch.SendPacket(new FlightStatusMessage { IsFlying = clientIsFlying });
            }
        }

        private void OnServerTick(float dt)
        {
            if (sapi == null) return;

            var channel = sapi.Network.GetChannel("claimflight");
            var claimApi = sapi.World.Claims;
            bool anyFlying = false;

            foreach (var player in sapi.World.AllOnlinePlayers)
            {
                if (player?.Entity == null) continue;
                if (player is not IServerPlayer splayer) continue;

                bool isFlying = playerFlightState.TryGetValue(splayer.PlayerUID, out var flying) && flying;
                if (isFlying) anyFlying = true;

                var pos = splayer.Entity.ServerPos.AsBlockPos;
                bool allowed = false;
                try
                {
                    var claims = claimApi.Get(pos);
                    allowed = claims != null && claims.Length > 0 && claimApi.TryAccess(splayer, pos, EnumBlockAccessFlags.BuildOrBreak);
                }
                catch { }

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
                                        Lang.Get("claimflight:leave_claim_timer", displaySeconds) ??
                                        $"Flight will disable in {displaySeconds} seconds",
                                        EnumChatType.Notification);
                                }, "claimflight_warn");
                            }

                            secondsLeft--;

                            if (secondsLeft <= 0)
                            {
                                sapi!.Event.EnqueueMainThreadTask(() =>
                                {
                                    DisableFlightServerSide(splayer, channel, true, "claimflight:flight_disabled_outside_claim");
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

            if (!anyFlying)
            {
                UnregisterServerTick();
            }
        }

        private void EnableFlightServerSide(IServerPlayer player, IServerNetworkChannel channel, bool sendMessage = false, string? customMessageKey = null)
        {
            if (player.Entity is not EntityPlayer entityPlayer) return;
            RegisterServerTick();

            player.WorldData.FreeMove = true;
            player.WorldData.NoClip = false;
            playerFlightState[player.PlayerUID] = true;
            lastInsideClaim[player.PlayerUID] = DateTime.UtcNow;
            entityPlayer.Properties.FallDamageMultiplier = 0f;

            channel.SendPacket(new FlightStatusMessage { IsFlying = true }, player);
            sapi?.Logger.Notification($"[ClaimFlight] Enabled flight for {player.PlayerName}");

            if (sendMessage)
            {
                string messageKey = customMessageKey ?? "claimflight:flight_enabled";
                player.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get(messageKey) ?? "Flight enabled!", EnumChatType.Notification);
            }
        }

        private void DisableFlightServerSide(IServerPlayer player, IServerNetworkChannel channel, bool sendMessage = false, string? customMessageKey = null)
        {
            if (player.Entity is not EntityPlayer entityPlayer) return;
            sapi?.Logger.Notification($"[ClaimFlight] Disabled flight for {player.PlayerName}");

            entityPlayer.Properties.FallDamageMultiplier = 0f;
            player.WorldData.FreeMove = false;
            player.WorldData.NoClip = false;
            playerFlightState[player.PlayerUID] = false;
            channel.SendPacket(new FlightStatusMessage { IsFlying = false }, player);

            var restoreTimer = new System.Timers.Timer(PostFlightSafeSeconds * 1000);
            restoreTimer.AutoReset = false;
            restoreTimer.Elapsed += (s, e) =>
            {
                restoreTimer.Stop();
                restoreTimer.Dispose();
                sapi?.Event.EnqueueMainThreadTask(() =>
                {
                    entityPlayer.Properties.FallDamageMultiplier = 1f;
                }, "restoreFallDamage");
            };
            restoreTimer.Start();

            if (sendMessage)
            {
                string messageKey = customMessageKey ?? "claimflight:flight_disabled";
                player.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get(messageKey) ?? "Flight disabled!", EnumChatType.Notification);
            }
        }
    }
}