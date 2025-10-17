using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace storylocationregen
{
    public sealed class StoryLocationRegenModSystem : ModSystem
    {
        private ICoreServerAPI api;
        private string configFilePath;

        private Dictionary<string, LocationData> locations = new();

        private const int MinRegenDays = 7;
        private const int MaxRegenDays = 14;
        private Random rng = new();

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            string worldName = api.WorldManager?.CurrentWorldName ?? "default";
            configFilePath = Path.Combine(api.DataBasePath, $"{worldName}_storyloc_regen.json");

            LoadConfig();
            RegisterCommands(api);

            api.Event.RegisterGameTickListener(OnGameTick, 10000);
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            var root = api.ChatCommands.Create("storylocregen")
                .WithDescription(Lang.Get("storylocationregen:command_root_desc"))
                .RequiresPrivilege(Privilege.controlserver);

            root.BeginSubCommand("set")
                .WithDescription(Lang.Get("storylocationregen:set_desc"))
                .WithArgs(api.ChatCommands.Parsers.Word("locationName"), api.ChatCommands.Parsers.Int("chunkRadius"))
                .HandleWith(OnSetCmd);

            root.BeginSubCommand("regen")
                .WithDescription(Lang.Get("storylocationregen:regen_desc"))
                .WithArgs(api.ChatCommands.Parsers.Word("locationName"))
                .HandleWith(OnRegenCmd);

            root.BeginSubCommand("forceregen")
                .WithDescription(Lang.Get("storylocationregen:forceregen_desc"))
                .WithArgs(api.ChatCommands.Parsers.Word("locationName"))
                .HandleWith(OnForceRegenCmd);

            root.BeginSubCommand("delete")
                .WithDescription(Lang.Get("storylocationregen:delete_desc"))
                .WithArgs(api.ChatCommands.Parsers.Word("locationName"))
                .HandleWith(OnDeleteLocationCmd);

            root.BeginSubCommand("list")
                .WithDescription(Lang.Get("storylocationregen:list_desc"))
                .HandleWith(OnListCmd);
        }

        private void LoadConfig()
        {
            if (!File.Exists(configFilePath)) return;

            try
            {
                var modData = JsonConvert.DeserializeObject<ModData>(File.ReadAllText(configFilePath));
                if (modData == null) return;

                locations = modData.Locations ?? new();
            }
            catch (Exception ex)
            {
                api.Logger.Error(Lang.Get("storylocationregen:error_loading_config", ex.Message));
            }
        }

        private void SaveConfig()
        {
            try
            {
                var modData = new ModData
                {
                    Locations = locations
                };

                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(modData, Formatting.Indented));
                api.Logger.Notification(Lang.Get("storylocationregen:config_saved"));
            }
            catch (Exception ex)
            {
                api.Logger.Error(Lang.Get("storylocationregen:error_saving_config", ex.Message));
            }
        }

        private TextCommandResult OnSetCmd(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            if (player == null)
                return TextCommandResult.Error(Lang.Get("storylocationregen:player_not_found"));

            if (args.Parsers.Count < 2)
                return TextCommandResult.Error(Lang.Get("storylocationregen:usage_set"));

            string name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name))
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_empty_name"));

            if (!int.TryParse(args.Parsers[1].GetValue()?.ToString(), out int chunkRadius) || chunkRadius <= 0)
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_invalid_radius"));

            var pos = player.Entity.Pos;
            int blockX = (int)pos.X;
            int blockZ = (int)pos.Z;

            int regenIntervalDays = rng.Next(MinRegenDays, MaxRegenDays + 1);

            locations[name] = new LocationData
            {
                X = blockX,
                Z = blockZ,
                ChunkSize = chunkRadius,
                LastRegenDay = (long)api.World.Calendar.TotalDays,
                RegenIntervalDays = regenIntervalDays,
                IsResetScheduled = false
            };

            SaveConfig();

            string msg = Lang.Get("storylocationregen:coords_saved", name, blockX, blockZ, regenIntervalDays);
            api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);

            return TextCommandResult.Success();
        }

        private TextCommandResult OnRegenCmd(TextCommandCallingArgs args)
        {
            if (args.Parsers.Count < 1)
                return TextCommandResult.Error(Lang.Get("storylocationregen:usage_regen"));

            string name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || !locations.TryGetValue(name, out var loc))
                return TextCommandResult.Error(Lang.Get("storylocationregen:coords_not_saved", name));

            if (loc.IsResetScheduled)
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_already_scheduled"));

            loc.LastRegenDay = (long)api.World.Calendar.TotalDays;
            SaveConfig();
            PreRegen(name, loc);

            return TextCommandResult.Success(Lang.Get("storylocationregen:forced_regen", name));
        }

        private TextCommandResult OnForceRegenCmd(TextCommandCallingArgs args)
        {
            if (args.Parsers.Count < 1)
                return TextCommandResult.Error(Lang.Get("storylocationregen:usage_forceregen"));

            string name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || !locations.TryGetValue(name, out var loc))
                return TextCommandResult.Error(Lang.Get("storylocationregen:coords_not_saved", name));

            if (loc.IsResetScheduled)
                return TextCommandResult.Error(Lang.Get("storylocationregen:error_already_scheduled"));

            loc.LastRegenDay = (long)api.World.Calendar.TotalDays;
            SaveConfig();
            ForcePreRegen(name, loc);

            return TextCommandResult.Success(Lang.Get("storylocationregen:forced_regen", name));
        }

        private TextCommandResult OnDeleteLocationCmd(TextCommandCallingArgs args)
        {
            if (args.Parsers.Count < 1)
                return TextCommandResult.Error(Lang.Get("storylocationregen:usage_delete"));

            string name = args.Parsers[0].GetValue()?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name) || !locations.ContainsKey(name))
                return TextCommandResult.Error(Lang.Get("storylocationregen:coords_not_saved", name));

            var loc = locations[name];
            loc.IsResetScheduled = false;

            locations.Remove(name);
            SaveConfig();

            return TextCommandResult.Success(Lang.Get("storylocationregen:location_deleted", name));
        }

        private TextCommandResult OnListCmd(TextCommandCallingArgs args)
        {
            if (locations.Count == 0)
                return TextCommandResult.Success(Lang.Get("storylocationregen:no_locations"));

            long totalDays = (long)api.World.Calendar.TotalDays;

            var lines = locations.Select(kvp =>
            {
                var loc = kvp.Value;
                long nextResetDay = loc.LastRegenDay + loc.RegenIntervalDays;
                long daysLeft = Math.Max(0, nextResetDay - totalDays);
                string scheduled = loc.IsResetScheduled ? Lang.Get("storylocationregen:scheduled") : "";

                return Lang.Get("storylocationregen:days_until_regen", kvp.Key, daysLeft) + scheduled;
            });

            return TextCommandResult.Success(string.Join("\n", lines));
        }

        private void PreRegen(string name, LocationData loc)
        {
            if (loc.IsResetScheduled) return;
            loc.IsResetScheduled = true;
            SaveConfig();

            api.World.RegisterCallback(dt => { SendFirstWarningMessage(loc, name); }, 0);
            api.World.RegisterCallback(dt => { SendSecondWarningMessage(loc); }, 600000);
            api.World.RegisterCallback(dt =>
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

            api.World.RegisterCallback(dt => { SendFirstWarningMessage(loc, name); }, 0);
            api.World.RegisterCallback(dt => { SendSecondWarningMessage(loc); }, 30000);
            api.World.RegisterCallback(dt =>
            {
                RegenArea(name, loc);
                loc.IsResetScheduled = false;
                loc.RegenIntervalDays = rng.Next(MinRegenDays, MaxRegenDays + 1);
                SaveConfig();
            }, 60000);
        }

        private void SendFirstWarningMessage(LocationData loc, string name)
        {
            int radiusBlocks = loc.ChunkSize * api.World.BlockAccessor.ChunkSize;
            foreach (var player in api.World.AllPlayers)
            {
                if (player?.Entity == null) continue;

                double dx = player.Entity.Pos.X - loc.X;
                double dz = player.Entity.Pos.Z - loc.Z;
                if ((dx * dx + dz * dz) <= radiusBlocks * radiusBlocks && player is IServerPlayer sp)
                {
                    sp.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:insidewarning1", name), EnumChatType.Notification);
                    sp.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:insidewarning2"), EnumChatType.Notification);
                }
            }
        }

        private void SendSecondWarningMessage(LocationData loc)
        {
            int radiusBlocks = loc.ChunkSize * api.World.BlockAccessor.ChunkSize;
            foreach (var player in api.World.AllPlayers)
            {
                if (player?.Entity == null) continue;

                double dx = player.Entity.Pos.X - loc.X;
                double dz = player.Entity.Pos.Z - loc.Z;
                if ((dx * dx + dz * dz) <= radiusBlocks * radiusBlocks && player is IServerPlayer sp)
                {
                    sp.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:insidewarning3"), EnumChatType.Notification);
                    sp.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:insidewarning4"), EnumChatType.Notification);
                }
            }
        }

        private void RegenArea(string name, LocationData loc)
        {
            api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:regen_started", name), EnumChatType.Notification);

            int radiusChunks = loc.ChunkSize;
            int centerChunkX = loc.X / api.World.BlockAccessor.ChunkSize;
            int centerChunkZ = loc.Z / api.World.BlockAccessor.ChunkSize;

            for (int x = centerChunkX - radiusChunks; x <= centerChunkX + radiusChunks; x++)
            {
                for (int z = centerChunkZ - radiusChunks; z <= centerChunkZ + radiusChunks; z++)
                {
                    double dx = (x * api.World.BlockAccessor.ChunkSize + api.World.BlockAccessor.ChunkSize / 2) - loc.X;
                    double dz = (z * api.World.BlockAccessor.ChunkSize + api.World.BlockAccessor.ChunkSize / 2) - loc.Z;
                    if ((dx * dx + dz * dz) > radiusChunks * radiusChunks * api.World.BlockAccessor.ChunkSize * api.World.BlockAccessor.ChunkSize)
                        continue;

                    api.WorldManager.DeleteChunkColumn(x, z);
                    api.WorldManager.CreateChunkColumnForDimension(x, z, 1);
                }
            }

            foreach (var player in api.World.AllPlayers)
            {
                if (player?.Entity == null) continue;

                double dx = player.Entity.Pos.X - loc.X;
                double dz = player.Entity.Pos.Z - loc.Z;
                if ((dx * dx + dz * dz) <= radiusChunks * api.World.BlockAccessor.ChunkSize * radiusChunks * api.World.BlockAccessor.ChunkSize && player is IServerPlayer sp)
                {
                    sp.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:warning_teleport"), EnumChatType.Notification);
                    sp.Entity.TeleportTo(sp.GetSpawnPosition(false));
                }
            }

            api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, Lang.Get("storylocationregen:regen_success", name), EnumChatType.Notification);
            SaveConfig();
        }

        private void OnGameTick(float dt)
        {
            long totalDays = (long)api.World.Calendar.TotalDays;

            foreach (var kvp in locations)
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