# ToastLib

A library mod leveraging the ImGui mod by Maltiez to draw toast style popups.

## Table of Contents
- [Requirements](#requirements)
- [VTML Support](#vtml-support)
    - [Supported Tags](#supported-tags)
- [Client-Side Usage](#client-side-usage-for-mod-developers)
    - [Import ToastLib](#1-import-toastlib)
    - [Get a Reference to ToastLib](#2-get-a-reference-to-toastlib)
    - [Display Toasts](#3-display-toasts)
        - [Simple Toast](#simple-toast)
        - [Toast with Placeholder/Arguments](#toast-with-placeholderarguments)
        - [Simple Toast using ShowToastAdv()](#simple-toast-using-showtoastadv)
        - [Toast with Placeholder/Arguments using ShowToastAdv()](#toast-with-placeholderarguments-using-showtoastadv)
- [Server-Side Usage](#server-side-usage-for-mod-developers)
    - [Import ToastLib](#1-import-toastlib-1)
    - [Get a Reference to ToastLib](#2-get-a-reference-to-toastlib-1)
    - [Display Toasts](#3-display-toasts-1)
        - [Simple Toast Displayed to ALL Online Players](#simple-toast-displayed-to-all-online-players)
        - [Simple Toast Displayed to a Single Online Player](#simple-toast-displayed-to-a-single-online-player)
- [Extended Client & Server Examples](#extended-client--server-examples)

## Requirements
- [ImGui](https://mods.vintagestory.at/imgui)

## VTML Support

ToastLib supports a **very limited subset of VTML** for basic text formatting and coloring.  
This makes it possible to style text in lightweight toasts without relying on full markup parsing.

### Supported Tags

| Tag | Description | Example | Result |
|-----|--------------|----------|---------|
| `<font color="lightcoral">` | Sets text color | `<font color="lightcoral">Alert</font>` | Alert (light coral) |
| `<font color="#00F2FF">` | Sets text color | `<font color="#00F2FF">Info</font>` | Info (#00F2FF) |
| `<font weight="bold">` | Makes text bold | `<font weight="bold">Important</font>` | **Important** |
| `<strong>` | Alias for bold text | `<strong>Warning</strong>` | **Warning** |
| `<br>` | Inserts a line break | `Line 1<br>Line 2` | Line 1<br>Line 2 |

#### Notes
- Only **font color**, **font weight**, **strong**, and **br** tags are supported.  
- Font colors accept **named colors** and **hex color codes**, hex codes support both 6 and 8 digit codes. (e.g., `lightcoral`, `#00F2FF`, `#00F2FF7F`).
- `<br>` adds vertical spacing between lines.
- Example of combining features:

```csharp
toastLib.ShowToast("<font color=\"lightgreen\">Colored Text: lightgreen</font> <br><br> <font color=\"#00f2ff\">Colored Text: #00f2ff</font> <br><br> <strong>strong</strong> <br><br> Line break");
```

## Client-Side Usage for Mod Developers

### 1. Import ToastLib

Include the ToastLib namespace in your mod:

```csharp
using toastlib;
```

---

### 2. Get a Reference to ToastLib

In your `StartClientSide` method, retrieve the ToastLib mod system from the mod loader:

```csharp
private ICoreClientAPI capi = null!;
private toastlibModSystem toastLib = null!;

public override void StartClientSide(ICoreClientAPI capi)
{
    this.capi = capi;
    toastLib = capi.ModLoader.GetModSystem<toastlibModSystem>();
}
```

---

### 3. Display Toasts

*Examples use `Lang.Get` as it's the ideal way to handle strings, while ToastLib can work with plain text strings, it is preferred to use `Lang.Get`.*

---

### Simple Toast

**Language Entry:**

```json
{
  "mymodname:hexcode_fontcolor": "<font color=\"#00F2FF\">This is an example of a toast using the font color #00F2FF.</font>",
}
```

**How to Display:**

```csharp
toastLib.ShowToast(Lang.Get("mymodname:hexcode_fontcolor"));
```

**Display Result:**

The toast will show:  
"This is an example of a toast using the font color #00F2FF."

This will display in a Cyan/Light Blue text color.

---

### Toast with Placeholder/Arguments

**Language Entry:**

```json
{
  "mymodname:placeholder_toast": "<font color=\"lightgoldenrodyellow\">This toast includes a placeholder to show this number: {0}</font>"
}
```

**How to Display:**

```csharp
string placeholderTxt = "69";

string message = Lang.Get("mymodname:placeholder_toast", placeholderTxt);

toastLib.ShowToast(message);
```

**Display Result:**

The toast will show:  
"This toast includes a placeholder to show this number: 69"

This will display in the lightgoldenrodyellow text color.

---

### Simple Toast using `ShowToastAdv()`

*ToastLib now includes a new method `ShowToastAdv()` that allows more customization of the toasts. Using this method instead of `ShowToast()` allows you to set the time (in ms) the toast displays for and the background color of the toast. It is highly recommended to use 8 digit hex codes for the background color as transparency is needed for the best looking toasts.*


**Language Entry:**

```json
{
  "mymodname:hexcode_fontcolor": "<font color=\"#00F2FF\">This is an example of a toast using the font color #00F2FF.</font>",
}
```

**How to Display:**

```csharp
toastLib.ShowToastAdv(Lang.Get("mymodname:hexcode_fontcolor"), 8000f, "#00F2FF7F");
```

**Display Result:**

The toast will show:  
"This is an example of a toast using the font color #00F2FF."

This will display in a Cyan/Light Blue text color with an opaque cyan/teal toast background and it will last for 8 seconds.

---

### Toast with Placeholder/Arguments using `ShowToastAdv()`

**Language Entry:**

```json
{
  "mymodname:placeholder_toast": "<font color=\"lightgoldenrodyellow\">This toast includes a placeholder to show this number: {0}</font>"
}
```

**How to Display:**

```csharp
string placeholderTxt = "69";

string message = Lang.Get("mymodname:placeholder_toast", placeholderTxt);

toastLib.ShowToastAdv(message, 8000f, "#00F2FF7F");
```

**Display Result:**

The toast will show:  
"This toast includes a placeholder to show this number: 69"

This will display in the lightgoldenrodyellow text color with an opaque cyan/teal toast background and it will last for 8 seconds.

---

## Server-Side Usage for Mod Developers

### 1. Import ToastLib

Include the ToastLib namespace in your mod:

```csharp
using toastlib;
```

---

### 2. Get a Reference to ToastLib

In your `StartServerSide` method, retrieve the ToastLib mod system from the mod loader:

```csharp
private ICoreServerAPI sapi = null!;
private toastlibModSystem toastLib = null!;

public override void StartServerSide(ICoreServerAPI sapi)
{
    this.sapi = sapi;
    toastLib = sapi.ModLoader.GetModSystem<toastlibModSystem>();
}
```

---

### 3. Display Toasts

*Examples use `Lang.Get` as it's the ideal way to handle strings, while ToastLib can work with plain text strings, it is preferred to use `Lang.Get`.*

---

### Simple Toast Displayed to ALL Online Players

**Language Entry:**

```json
{
  "mymodname:named_fontcolor": "<font color=\"yellow\">This is an example of a toast using the font color yellow.</font>",
}
```

**How to Display:**

```csharp
toastLib.Server.ShowToast(Lang.Get("mymodname:named_fontcolor"));
```

**Display Result:**

The toast will show:  
"This is an example of a toast using the font color yellow."

This will display in a Yellow text color.

---

### Simple Toast Displayed to a Single Online Player

**Language Entry:**

```json
{
  "mymodname:named_fontcolor2": "<font color=\"lightgreen\">This is an example of a toast using the font color lightgreen.</font>",
}
```

**How to Display:**

```csharp
toastLib.Server.ShowToast(player, Lang.Get("mymodname:named_fontcolor2"));
```

**Display Result:**

The toast will show:  
"This is an example of a toast using the font color lightgreen."

This will display in a Light Green text color.

---

## Extended Client & Server Examples

For more examples you can view the source code from ToastLib where the lang keys are set and built-in commands are registered.

**en.json:**
```json
{
  "toastlib:example_cmd_desc": "Display an example toast.",
  "toastlib:wrap_cmd_desc": "Display an example toast using word wrapping.",
  "toastlib:placeholder_cmd_desc": "Display an example toast that uses a placeholder in the lang entry.",
  "toastlib:placeholderadv_cmd_desc": "Display an example toast that uses a placeholder in the lang entry also using ShowToastAdv.",
  "toastlib:multi_cmd_desc": "Display multiple example toasts.",
  "toastlib:multiadv_cmd_desc": "Display multiple example toasts using ShowToastAdv.",

  "toastlib:example_toast": "<font color=\"lightgreen\">Colored Text: lightgreen</font><br><br><font color=\"#00F2FF\">Colored Text: #00F2FF</font><br><br><strong>strong</strong><br><br>Line break",
  "toastlib:wrap_toast": "<font color=\"lightgoldenrodyellow\">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec scelerisque auctor tortor, sed ornare magna egestas eget. Pellentesque nec velit.</font>",
  "toastlib:placeholder_toast": "<font color=\"lightgoldenrodyellow\">This toast includes a placeholder to show this number: {0}</font>",

  "toastlib:multi_toast1": "<font color=\"yellow\">This is an example of a toast using the font color yellow.</font>",
  "toastlib:multi_toast2": "<font color=\"#00F2FF\">This is an example of a toast using the font color #00F2FF.</font>",
  "toastlib:multi_toast3": "<font color=\"#00F2FF7F\">This is an example of a toast using the font color #00F2FF7F, this should have a lower opacity.</font>",
  "toastlib:multi_toast4": "<font color=\"lightgreen\" weight=\"bold\">This is an example of a toast using the font color lightgreen and the font weight bold.</font>",
  "toastlib:multi_toast5": "This is an example of a toast using the <strong>strong</strong> tag only on one word.",
  "toastlib:multi_toast6": "This is an example of a toast using a line break<br>This is a new line after the line break.",
  "toastlib:multi_toast7": "This is an example of a long toast that causes the words to wrap instead of using a new line with the br tag. To increase the length of this I am mindlessly typing words while watching a tv show. It seems to work well."
}
```

**Commands.cs:**
```csharp
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
```


