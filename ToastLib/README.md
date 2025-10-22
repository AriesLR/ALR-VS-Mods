# ToastLib

A library mod leveraging the ImGui mod by Maltiez to draw toast style popups.

## Requirements
- [Dear ImGui](https://mods.vintagestory.at/imgui)

### VTML Support

ToastLib supports a **very limited subset of VTML** for basic text formatting and coloring.  
This makes it possible to style text in lightweight toasts without relying on full markup parsing.

#### Supported Tags

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

## Client and Server Usage for Mod Developers

### 1. Import ToastLib

Include the ToastLib namespace in your mod:

```csharp
using toastlib;
```

---

### 2. Get a Reference to ToastLib

In your `StartClientSide` method, retrieve the ToastLib mod system from the mod loader:

```csharp
private toastlib.toastlibModSystem? toastLib;
private ICoreClientAPI capi;

public override void StartClientSide(ICoreClientAPI api)
{
    capi = api;
    toastLib = capi.ModLoader.GetModSystem<toastlib.toastlibModSystem>();
}
```

- `toastLib` may be null if the mod is not installed. Always check for null before showing a toast.

---

### 3. Display a Simple Toast

Once you have a reference:

```csharp
if (toastLib != null)
{
    toastLib.ShowToast("This is a client-side message!");
}
```

- Use this to display notifications in response to hotkeys, GUI interactions, or other client-side triggers.

---

### 4. Display a Toast with Arguments or Placeholders

ToastLib supports **formatted messages** using arguments and **language key placeholders** (e.g., `{0}`, `{1}`, etc.).

You can pass arguments to `ShowToast()` just like `Lang.Get()` would handle them.  
ToastLib automatically substitutes placeholders in translation strings or plain text.

Example 1 — Using plain text formatting:

```csharp
if (toastLib != null)
{
    toastLib.ShowToast("You have {0} new messages.", 3);
}
```

This will display:  
“You have 3 new messages.”

Example 2 — Using a language key with placeholders:

Suppose your mod has a language entry:

```json
"mymodname:mytoast_welcome": "Welcome back, {0}!"
```

Then you can display it like this:

```csharp
if (toastLib != null)
{
    toastLib.ShowToast(Lang.Get("mymodname:mytoast_welcome", "PlayerName"));
}
```

The toast will show:  
“Welcome back, PlayerName!”

ToastLib handles these automatically through its internal formatting system, so you can safely mix direct strings and localization keys with arguments.

---

### 5. Server-to-Client Messaging (Overview)

ToastLib is **client-only** and should never be initialized or called directly on the server.  
If you want the server to trigger a toast on the client, you must:

- Send a custom packet (e.g., using **ProtoBuf**) from the server to the target client(s).
- Handle that packet on the client side.
- Within the client handler, call `ShowToast()` using the client’s reference to ToastLib.

Example flow:
1. Server sends packet → client receives message.
2. Client-side handler extracts the message and arguments.
3. Client calls `toastLib.ShowToast(message, args)`.

This ensures that ToastLib is only ever used on the client, trying to use ToastLib on the server will not work.

---
