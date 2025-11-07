using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace toastlib
{
    public static class Commands
    {
        // ============ Register Client-Side Commands ============
        public static void RegisterClient(ICoreClientAPI capi, toastlibModSystem toastLib)
        {
            var root = capi.ChatCommands.Create("toastlib")
                .WithDescription("Toastlib Commands")
                .RequiresPrivilege(Privilege.chat);

            root.BeginSubCommand("example")
                .WithDescription(Lang.Get("toastlib:example_cmd_desc"))
                .HandleWith(args => OnExampleCmdClient(args, toastLib));

            root.BeginSubCommand("wrap")
                .WithDescription(Lang.Get("toastlib:wrap_cmd_desc"))
                .HandleWith(args => OnWrapCmdClient(args, toastLib));

            root.BeginSubCommand("placeholder")
                .WithDescription(Lang.Get("toastlib:placeholder_cmd_desc"))
                .HandleWith(args => OnPlaceholderCmdClient(args, toastLib));

            root.BeginSubCommand("placeholderadv")
                .WithDescription(Lang.Get("toastlib:placeholderadv_cmd_desc"))
                .HandleWith(args => OnPlaceholderAdvCmdClient(args, toastLib));

            root.BeginSubCommand("multi")
                .WithDescription(Lang.Get("toastlib:multi_cmd_desc"))
                .HandleWith(args => OnMultiCmdClient(args, toastLib, capi));

            root.BeginSubCommand("multiadv")
                .WithDescription(Lang.Get("toastlib:multiadv_cmd_desc"))
                .HandleWith(args => OnMultiAdvCmdClient(args, toastLib, capi));
        }

        // ============ Client-Side Commands ============
        private static TextCommandResult OnExampleCmdClient(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            toastLib.ShowToast(Lang.Get("toastlib:example_toast"));
            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnWrapCmdClient(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            toastLib.ShowToast(Lang.Get("toastlib:wrap_toast"));
            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnPlaceholderCmdClient(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            string placeholderTxt = "69";

            string message = Lang.Get("toastlib:placeholder_toast", placeholderTxt);

            toastLib.ShowToast(message);
            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnPlaceholderAdvCmdClient(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            string placeholderTxt = "69";

            string message = Lang.Get("toastlib:placeholder_toast", placeholderTxt);

            toastLib.ShowToastAdv(message, 8000f, "#00F2FF7F");
            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnMultiCmdClient(TextCommandCallingArgs args, toastlibModSystem toastLib, ICoreClientAPI capi)
        {
            string[] keys = new[]
            {
                "toastlib:multi_toast1",
                "toastlib:multi_toast2",
                "toastlib:multi_toast3",
                "toastlib:multi_toast4",
                "toastlib:multi_toast5",
                "toastlib:multi_toast6",
                "toastlib:multi_toast7"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                int delayMs = i * 500;
                string key = keys[i];
                capi.Event.RegisterCallback(_ => toastLib.ShowToast(Lang.Get(key)), delayMs);
            }

            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnMultiAdvCmdClient(TextCommandCallingArgs args, toastlibModSystem toastLib, ICoreClientAPI capi)
        {
            string[] keys = new[]
            {
                "toastlib:multi_toast1",
                "toastlib:multi_toast2",
                "toastlib:multi_toast3",
                "toastlib:multi_toast4",
                "toastlib:multi_toast5",
                "toastlib:multi_toast6",
                "toastlib:multi_toast7"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                int delayMs = i * 500;
                string key = keys[i];
                capi.Event.RegisterCallback(_ => toastLib.ShowToastAdv(Lang.Get(key), 8000f, "#00F2FF7F"), delayMs);
            }

            return TextCommandResult.Success("");
        }

        // ============ Register Server-Side Commands ============
        public static void RegisterServer(ICoreServerAPI sapi, toastlibModSystem toastLib)
        {
            var root = sapi.ChatCommands.Create("toastlib")
                .WithDescription("Toastlib Commands")
                .RequiresPrivilege(Privilege.chat);

            root.BeginSubCommand("example")
                .WithDescription(Lang.Get("toastlib:example_cmd_desc"))
                .HandleWith(args => OnExampleCmdServer(args, toastLib));

            root.BeginSubCommand("wrap")
                .WithDescription(Lang.Get("toastlib:wrap_cmd_desc"))
                .HandleWith(args => OnWrapCmdServer(args, toastLib));

            root.BeginSubCommand("placeholder")
                .WithDescription(Lang.Get("toastlib:placeholder_cmd_desc"))
                .HandleWith(args => OnPlaceholderCmdServer(args, toastLib));

            root.BeginSubCommand("placeholderadv")
                .WithDescription(Lang.Get("toastlib:placeholderadv_cmd_desc"))
                .HandleWith(args => OnPlaceholderAdvCmdServer(args, toastLib));

            root.BeginSubCommand("multi")
                .WithDescription(Lang.Get("toastlib:multi_cmd_desc"))
                .HandleWith(args => OnMultiCmdServer(args, toastLib, sapi));

            root.BeginSubCommand("multiadv")
                .WithDescription(Lang.Get("toastlib:multiadv_cmd_desc"))
                .HandleWith(args => OnMultiAdvCmdServer(args, toastLib, sapi));
        }

        // ============ Server-Side Commands ============
        private static TextCommandResult OnExampleCmdServer(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            toastLib.Server?.ShowToast(Lang.Get("toastlib:example_toast"));
            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnWrapCmdServer(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            toastLib.Server?.ShowToast(Lang.Get("toastlib:wrap_toast"));
            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnPlaceholderCmdServer(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            if (args.Caller.Player == null) return TextCommandResult.Success("");

            if (args.Caller.Player is IServerPlayer serverPlayer)
            {
                string placeholderTxt = "69";
                string message = Lang.Get("toastlib:placeholder_toast", placeholderTxt);

                toastLib.Server?.ShowToast(serverPlayer, message);
            }

            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnPlaceholderAdvCmdServer(TextCommandCallingArgs args, toastlibModSystem toastLib)
        {
            if (args.Caller.Player == null) return TextCommandResult.Success("");

            if (args.Caller.Player is IServerPlayer serverPlayer)
            {
                string placeholderTxt = "69";
                string message = Lang.Get("toastlib:placeholder_toast", placeholderTxt);

                toastLib.Server?.ShowToastAdv(serverPlayer, message, 8000f, "#00F2FF7F");
            }

            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnMultiCmdServer(TextCommandCallingArgs args, toastlibModSystem toastLib, ICoreServerAPI sapi)
        {
            string[] keys = new[]
            {
                "toastlib:multi_toast1",
                "toastlib:multi_toast2",
                "toastlib:multi_toast3",
                "toastlib:multi_toast4",
                "toastlib:multi_toast5",
                "toastlib:multi_toast6",
                "toastlib:multi_toast7"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                int delayMs = i * 500;
                string key = keys[i];
                sapi.Event.RegisterCallback(_ => toastLib.Server?.ShowToast(Lang.Get(key)), delayMs);
            }

            return TextCommandResult.Success("");
        }

        private static TextCommandResult OnMultiAdvCmdServer(TextCommandCallingArgs args, toastlibModSystem toastLib, ICoreServerAPI sapi)
        {
            string[] keys = new[]
            {
                "toastlib:multi_toast1",
                "toastlib:multi_toast2",
                "toastlib:multi_toast3",
                "toastlib:multi_toast4",
                "toastlib:multi_toast5",
                "toastlib:multi_toast6",
                "toastlib:multi_toast7"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                int delayMs = i * 500;
                string key = keys[i];
                sapi.Event.RegisterCallback(_ => toastLib.Server?.ShowToastAdv(Lang.Get(key), 8000f, "#00F2FF7F"), delayMs);
            }

            return TextCommandResult.Success("");
        }
    }
}