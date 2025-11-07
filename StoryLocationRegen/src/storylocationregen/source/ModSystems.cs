using toastlib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace storylocationregen
{
    public sealed class StoryLocationRegenModSystem : ModSystem
    {
        private ICoreServerAPI sapi = null!;
        private ICoreClientAPI capi = null!;
        private toastlibModSystem toastLib = null!;

        private const string ConfigFilename = "StoryLocationRegen.json";
        private ModData config = new();

        private const int MinRegenDays = 7;
        private const int MaxRegenDays = 14;
        private readonly Random rng = new();

        // ============ Start Client-Side ============
        public override void StartClientSide(ICoreClientAPI capi)
        {
            this.capi = capi;
            toastLib = capi.ModLoader.GetModSystem<toastlibModSystem>();
        }

        // ============ Start Server-Side ============
        public override void StartServerSide(ICoreServerAPI sapi)
        {
            this.sapi = sapi;

            toastLib = sapi.ModLoader.GetModSystem<toastlibModSystem>();

            LoadConfig(sapi);
            RegisterCommands(sapi);

            sapi.Event.RegisterGameTickListener(OnGameTick, 10000);
        }

        // ============ Config Saving & Loading ============
        private void LoadConfig(ICoreServerAPI sapi)
        {
            var loaded = sapi.LoadModConfig<ModData>(ConfigFilename);
            if (loaded == null)
            {
                sapi.Logger.Notification("[Story Location Regen] No config found, creating default.");
                config = new ModData();
                SaveConfig();
            }
            else
            {
                config = loaded;
                if (config.Locations == null)
                    config.Locations = new();
                sapi.Logger.Notification("[Story Location Regen] Config loaded with {0} saved locations.", config.Locations.Count);
            }
        }

        private void SaveConfig()
        {
            if (sapi == null) return;
            sapi.StoreModConfig(config, ConfigFilename);
            sapi.Logger.Notification(Lang.Get("storylocationregen:log_config_saved"));
        }

        // ============ Register Commands ============
        private void RegisterCommands(ICoreServerAPI sapi)
        {
            var root = sapi.ChatCommands.Create("storylocregen")
                .WithDescription(Lang.Get("storylocationregen:command_root_desc"))
                .RequiresPrivilege(Privilege.controlserver);

            root.BeginSubCommand("set")
                .WithDescription(Lang.Get("storylocationregen:set_desc"))
                .WithArgs(sapi.ChatCommands.Parsers.Word("locationName"), sapi.ChatCommands.Parsers.Int("chunkRadius"))
                .HandleWith(OnSetCmd);

            root.BeginSubCommand("regen")
                .WithDescription(Lang.Get("storylocationregen:regen_desc"))
                .WithArgs(sapi.ChatCommands.Parsers.Word("locationName"))
                .HandleWith(OnRegenCmd);

            root.BeginSubCommand("forceregen")
                .WithDescription(Lang.Get("storylocationregen:forceregen_desc"))
                .WithArgs(sapi.ChatCommands.Parsers.Word("locationName"))
                .HandleWith(OnForceRegenCmd);

            root.BeginSubCommand("delete")
                .WithDescription(Lang.Get("storylocationregen:delete_desc"))
                .WithArgs(sapi.ChatCommands.Parsers.Word("locationName"))
                .HandleWith(OnDeleteLocationCmd);

            root.BeginSubCommand("list")
                .WithDescription(Lang.Get("storylocationregen:list_desc"))
                .HandleWith(OnListCmd);
        }

        // ============ StoryLocationRegen Commands ============
        private TextCommandResult OnSetCmd(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            if (player == null) return TextCommandResult.Error(Lang.Get("storylocationregen:player_not_found"));

            if (args.Parsers.Count < 2) return TextCommandResult.Error(Lang.Get("storylocationregen:usage_set"));

            string? name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name)) return TextCommandResult.Error(Lang.Get("storylocationregen:error_empty_name"));

            if (!int.TryParse(args.Parsers[1].GetValue()?.ToString(), out int chunkRadius) || chunkRadius <= 0)
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_invalid_radius"));

            var pos = player.Entity.Pos;
            int blockX = (int)pos.X;
            int blockZ = (int)pos.Z;

            int regenIntervalDays = rng.Next(MinRegenDays, MaxRegenDays + 1);

            config.Locations[name] = new LocationData
            {
                X = blockX,
                Z = blockZ,
                ChunkSize = chunkRadius,
                LastRegenDay = (long)sapi!.World.Calendar.TotalDays,
                RegenIntervalDays = regenIntervalDays,
                IsResetScheduled = false
            };

            SaveConfig();

            toastLib.Server.ShowToastAdv(Lang.Get("storylocationregen:coords_saved", name, blockX, blockZ, regenIntervalDays), 10000f, "#000000CC");

            return TextCommandResult.Success();
        }

        private TextCommandResult OnRegenCmd(TextCommandCallingArgs args)
        {
            if (args.Parsers.Count < 1) return TextCommandResult.Error(Lang.Get("storylocationregen:usage_regen"));

            string? name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || !config.Locations.TryGetValue(name, out var loc))
                return TextCommandResult.Error(Lang.Get("storylocationregen:coords_not_saved", name));

            if (loc.IsResetScheduled)
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_already_scheduled"));

            loc.LastRegenDay = (long)sapi.World.Calendar.TotalDays;
            SaveConfig();
            PreRegen(name, loc);

            return TextCommandResult.Success(Lang.Get("storylocationregen:forced_regen", name));
        }

        private TextCommandResult OnForceRegenCmd(TextCommandCallingArgs args)
        {
            if (args.Parsers.Count < 1) return TextCommandResult.Error(Lang.Get("storylocationregen:usage_forceregen"));

            string? name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || !config.Locations.TryGetValue(name, out var loc))
                return TextCommandResult.Error(Lang.Get("storylocationregen:coords_not_saved", name));

            if (loc.IsResetScheduled)
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_already_scheduled"));

            loc.LastRegenDay = (long)sapi.World.Calendar.TotalDays;
            SaveConfig();
            ForcePreRegen(name, loc);

            return TextCommandResult.Success(Lang.Get("storylocationregen:forced_regen", name));
        }

        private TextCommandResult OnDeleteLocationCmd(TextCommandCallingArgs args)
        {
            if (args.Parsers.Count < 1) return TextCommandResult.Error(Lang.Get("storylocationregen:usage_delete"));

            string? name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || !config.Locations.ContainsKey(name))
                return TextCommandResult.Error(Lang.Get("storylocationregen:coords_not_saved", name));

            config.Locations.Remove(name);
            SaveConfig();

            return TextCommandResult.Success(Lang.Get("storylocationregen:location_deleted", name));
        }

        private TextCommandResult OnListCmd(TextCommandCallingArgs args)
        {
            if (config.Locations.Count == 0)
                return TextCommandResult.Success(Lang.Get("storylocationregen:no_locations"));

            long totalDays = (long)sapi.World.Calendar.TotalDays;

            var lines = config.Locations.Select(kvp =>
            {
                var loc = kvp.Value;
                long nextResetDay = loc.LastRegenDay + loc.RegenIntervalDays;
                long daysLeft = Math.Max(0, nextResetDay - totalDays);
                string scheduled = loc.IsResetScheduled ? Lang.Get("storylocationregen:scheduled") : "";
                return Lang.Get("storylocationregen:days_until_regen", kvp.Key, daysLeft) + scheduled;
            });

            return TextCommandResult.Success(string.Join("\n", lines));
        }

        // ============ StoryLocationRegen Logic ============
        private void PreRegen(string name, LocationData loc)
        {
            if (loc.IsResetScheduled) return;
            loc.IsResetScheduled = true;
            SaveConfig();

            sapi.World.RegisterCallback(dt => SendFirstWarningMessage(loc, name), 0);
            sapi.World.RegisterCallback(dt => SendSecondWarningMessage(loc), 600000);
            sapi.World.RegisterCallback(dt =>
            {
                RegenArea(name, loc);
                loc.IsResetScheduled = false;
                loc.RegenIntervalDays = rng.Next(MinRegenDays, MaxRegenDays + 1);
                SaveConfig();
            }, 900000);
        }

        private void ForcePreRegen(string name, LocationData loc)
        {
            if (loc.IsResetScheduled) return;
            loc.IsResetScheduled = true;
            SaveConfig();

            sapi.World.RegisterCallback(dt => SendFirstWarningMessage(loc, name), 0);
            sapi.World.RegisterCallback(dt => SendSecondWarningMessage(loc), 30000);
            sapi.World.RegisterCallback(dt =>
            {
                RegenArea(name, loc);
                loc.IsResetScheduled = false;
                loc.RegenIntervalDays = rng.Next(MinRegenDays, MaxRegenDays + 1);
                SaveConfig();
            }, 60000);
        }

        private void RegenArea(string name, LocationData loc)
        {
            toastLib.ShowToastAdv(Lang.Get("storylocationregen:regen_started", name), 10000f, "#000000CC");

            int radiusChunks = loc.ChunkSize;
            int centerChunkX = loc.X / GlobalConstants.ChunkSize;
            int centerChunkZ = loc.Z / GlobalConstants.ChunkSize;

            for (int x = centerChunkX - radiusChunks; x <= centerChunkX + radiusChunks; x++)
            {
                for (int z = centerChunkZ - radiusChunks; z <= centerChunkZ + radiusChunks; z++)
                {
                    double dx = (x * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2) - loc.X;
                    double dz = (z * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2) - loc.Z;
                    if ((dx * dx + dz * dz) > radiusChunks * radiusChunks * GlobalConstants.ChunkSize * GlobalConstants.ChunkSize)
                        continue;

                    sapi.WorldManager.DeleteChunkColumn(x, z);
                    sapi.WorldManager.CreateChunkColumnForDimension(x, z, 1);
                }
            }

            foreach (var player in sapi.World.AllPlayers)
            {
                if (player?.Entity == null) continue;
                double dx = player.Entity.Pos.X - loc.X;
                double dz = player.Entity.Pos.Z - loc.Z;
                if ((dx * dx + dz * dz) <= radiusChunks * GlobalConstants.ChunkSize * radiusChunks * GlobalConstants.ChunkSize && player is IServerPlayer sp)
                {
                    toastLib.Server.ShowToastAdv(sp, Lang.Get("storylocationregen:warning_teleport"), 10000f, "#000000CC");
                    sp.Entity.TeleportTo(sp.GetSpawnPosition(false));
                }
            }

            toastLib.Server.ShowToastAdv(Lang.Get("storylocationregen:regen_success", name), 10000f, "#000000CC");
            SaveConfig();
        }

        private void OnGameTick(float dt)
        {
            long totalDays = (long)sapi.World.Calendar.TotalDays;

            foreach (var kvp in config.Locations)
            {
                var loc = kvp.Value;
                long daysSinceReset = totalDays - loc.LastRegenDay;
                if (daysSinceReset >= loc.RegenIntervalDays && !loc.IsResetScheduled)
                {
                    PreRegen(kvp.Key, loc);
                    loc.LastRegenDay = totalDays;
                    SaveConfig();
                }
            }
        }

        // ============ Send Warning Messages ============
        private void SendFirstWarningMessage(LocationData loc, string name)
        {
            int radiusBlocks = loc.ChunkSize * GlobalConstants.ChunkSize;
            foreach (var player in sapi.World.AllPlayers)
            {
                if (player?.Entity == null) continue;
                double dx = player.Entity.Pos.X - loc.X;
                double dz = player.Entity.Pos.Z - loc.Z;
                if ((dx * dx + dz * dz) <= radiusBlocks * radiusBlocks && player is IServerPlayer sp)
                {
                    var messages = new (string Key, object?[] Args)[]
                    {
                        ("storylocationregen:insidewarning1", new object[] { name }),
                        ("storylocationregen:insidewarning2", Array.Empty<object>())
                    };

                    for (int i = 0; i < messages.Length; i++)
                    {
                        int delayMs = i * 2000;
                        var msg = messages[i];
                        sapi.Event.RegisterCallback(_ =>
                        {
                            string text = Lang.Get(msg.Key, msg.Args);
                            toastLib.Server.ShowToastAdv(sp, text, 10000f, "#000000CC");
                        }, delayMs);
                    }
                }
            }
        }

        private void SendSecondWarningMessage(LocationData loc)
        {
            int radiusBlocks = loc.ChunkSize * GlobalConstants.ChunkSize;
            foreach (var player in sapi.World.AllPlayers)
            {
                if (player?.Entity == null) continue;
                double dx = player.Entity.Pos.X - loc.X;
                double dz = player.Entity.Pos.Z - loc.Z;
                if ((dx * dx + dz * dz) <= radiusBlocks * radiusBlocks && player is IServerPlayer sp)
                {
                    var messages = new (string Key, object?[] Args)[]
                    {
                        ("storylocationregen:insidewarning3", Array.Empty<object>()),
                        ("storylocationregen:insidewarning4", Array.Empty<object>())
                    };

                    for (int i = 0; i < messages.Length; i++)
                    {
                        int delayMs = i * 2000;
                        var msg = messages[i];
                        sapi.Event.RegisterCallback(_ =>
                        {
                            string text = Lang.Get(msg.Key, msg.Args);
                            toastLib.Server.ShowToastAdv(sp, text, 10000f, "#000000CC");
                        }, delayMs);
                    }
                }
            }
        }

        // ============ Config Data ============
        private class LocationData
        {
            public int X { get; set; }
            public int Z { get; set; }
            public int ChunkSize { get; set; }
            public long LastRegenDay { get; set; }
            public int RegenIntervalDays { get; set; }
            public bool IsResetScheduled { get; set; }
        }

        private class ModData
        {
            public Dictionary<string, LocationData> Locations { get; set; } = new();
        }
    }
}