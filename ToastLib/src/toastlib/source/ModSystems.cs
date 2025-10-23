using ImGuiNET;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using VSImGui;
using VSImGui.API;

namespace toastlib
{
    public sealed class toastlibModSystem : ModSystem
    {
        private ICoreClientAPI? capi;
        private ImGuiModSystem? _modSystem;
        private readonly List<Toast> toastQueue = new();

        private const float ToastWidth = 400f;
        private const float ToastHeight = 10f;
        private const float SlideTimeMs = 500f;
        private const float DisplayTimeMs = 5000f;
        private const float Padding = 6f;

        public void ShowToast(string langKey, params object[] args)
        {
            if (capi == null) return;
            string text = FormatLang(langKey, args);
            toastQueue.Add(new Toast(text));
        }

        public void ShowToastAdv(string langKey, float? displayTimeMs = null, string? bgColor = null, params object[] args)
        {
            if (capi == null) return;
            string text = FormatLang(langKey, args);

            Vector4 color = bgColor != null ? ParseColor(bgColor) : new Vector4(0, 0, 0, 0.8f);
            float display = displayTimeMs ?? DisplayTimeMs;

            toastQueue.Add(new Toast(text, display, color));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            _modSystem = api.ModLoader.GetModSystem<ImGuiModSystem>();
            _modSystem.Draw += OnDrawToasts;
            RegisterCommands(api);
        }

        private void RegisterCommands(ICoreClientAPI api)
        {
            var root = api.ChatCommands.Create("toastlib")
                .WithDescription("Toastlib Commands")
                .RequiresPrivilege(Privilege.chat);

            root.BeginSubCommand("example")
                .WithDescription(Lang.Get("toastlib:example_cmd_desc"))
                .HandleWith(OnExampleCmd);

            root.BeginSubCommand("wrap")
                .WithDescription(Lang.Get("toastlib:wrap_cmd_desc"))
                .HandleWith(OnWrapCmd);

            root.BeginSubCommand("placeholder")
                .WithDescription(Lang.Get("toastlib:placeholder_cmd_desc"))
                .HandleWith(OnPlaceholderCmd);

            root.BeginSubCommand("placeholderadv")
                .WithDescription(Lang.Get("toastlib:placeholderadv_cmd_desc"))
                .HandleWith(OnPlaceholderAdvCmd);

            root.BeginSubCommand("multi")
                .WithDescription(Lang.Get("toastlib:multi_cmd_desc"))
                .HandleWith(OnMultiCmd);

            root.BeginSubCommand("multiadv")
                .WithDescription(Lang.Get("toastlib:multiadv_cmd_desc"))
                .HandleWith(OnMultiAdvCmd);
        }

        private TextCommandResult OnExampleCmd(TextCommandCallingArgs args)
        {
            ShowToast(Lang.Get("toastlib:example_toast"));
            return TextCommandResult.Success("");
        }

        private TextCommandResult OnWrapCmd(TextCommandCallingArgs args)
        {
            ShowToast(Lang.Get("toastlib:wrap_toast"));
            return TextCommandResult.Success("");
        }

        private TextCommandResult OnPlaceholderCmd(TextCommandCallingArgs args)
        {
            var placeholderTxt = "69";

            ShowToast(Lang.Get("toastlib:placeholder_toast"), placeholderTxt);
            return TextCommandResult.Success("");
        }

        private TextCommandResult OnPlaceholderAdvCmd(TextCommandCallingArgs args)
        {
            var placeholderTxt = "69";

            ShowToastAdv(Lang.Get("toastlib:placeholder_toast"), 8000f, "#00F2FF7F", placeholderTxt);
            return TextCommandResult.Success("");
        }

        private TextCommandResult OnMultiCmd(TextCommandCallingArgs args)
        {
            if (capi == null) return TextCommandResult.Success("");

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

                capi.Event.RegisterCallback(_ =>
                {
                    ShowToast(Lang.Get(key));
                }, delayMs);
            }

            return TextCommandResult.Success("");
        }

        private TextCommandResult OnMultiAdvCmd(TextCommandCallingArgs args)
        {
            if (capi == null) return TextCommandResult.Success("");

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

                capi.Event.RegisterCallback(_ =>
                {
                    ShowToastAdv(Lang.Get(key), 8000f, "#00F2FF7F");
                }, delayMs);
            }

            return TextCommandResult.Success("");
        }

        private CallbackGUIStatus OnDrawToasts(float deltaSeconds)
        {
            if (toastQueue.Count == 0) return CallbackGUIStatus.Closed;

            DrawToasts(deltaSeconds);
            return CallbackGUIStatus.DontGrabMouse;
        }

        private void DrawToasts(float deltaTime)
        {
            if (toastQueue.Count == 0) return;

            var drawList = ImGui.GetForegroundDrawList();
            var font = ImGui.GetFont();

            float baseX = 10f;
            float baseY = 10f;
            float spacing = 10f;

            var heights = new float[toastQueue.Count];
            for (int i = 0; i < toastQueue.Count; i++)
            {
                Vector2 textSize = MeasureVTMLText(font, toastQueue[i].Text, ToastWidth - 2 * Padding);
                heights[i] = Math.Max(ToastHeight, textSize.Y + 2 * Padding);
            }

            float targetY = baseY;
            for (int i = toastQueue.Count - 1; i >= 0; i--)
            {
                var t = toastQueue[i];
                t.ElapsedMs += deltaTime * 1000f;

                float toastHeight = heights[i];

                float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);

                float EaseOutBack(float t)
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
                }

                float EaseInBack(float t)
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }

                float targetX = baseX;
                float startX = -ToastWidth;
                float endX = -ToastWidth;

                float x;
                if (t.ElapsedMs < SlideTimeMs)
                {
                    x = MathHelper.Lerp(startX, targetX, EaseOutBack(MathF.Min(t.ElapsedMs / SlideTimeMs, 1f)));
                }
                else if (t.ElapsedMs > t.DisplayTimeMs)
                {
                    float tSlideOut = MathF.Min((t.ElapsedMs - t.DisplayTimeMs) / SlideTimeMs, 1f);
                    x = MathHelper.Lerp(targetX, endX, EaseInBack(tSlideOut));
                }
                else
                {
                    x = targetX;
                }

                float yOffset = 10f * (1 - EaseOutCubic(MathF.Min(t.ElapsedMs / SlideTimeMs, 1f)));

                t.CurrentY = MathHelper.Lerp(t.CurrentY, targetY + yOffset, 0.15f);

                Vector2 pos = new(x, t.CurrentY);
                Vector2 size = new(ToastWidth, toastHeight);

                drawList.AddRectFilled(pos, pos + size,
                    ImGui.ColorConvertFloat4ToU32(t.BackgroundColor), 4f);

                DrawVTML(drawList, font, pos + new Vector2(Padding, Padding),
                    t.Text, ToastWidth - 2 * Padding);

                targetY += toastHeight + spacing;

                if (t.ElapsedMs > t.DisplayTimeMs + SlideTimeMs)
                {
                    toastQueue.RemoveAt(i);
                }
            }
        }

        private IEnumerable<(string? Word, Vector4 Color, bool Bold, bool NewLine)> ParseVTML(string text)
        {
            string[] lines = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase).Split('\n');
            bool isFirstWord = true;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                var regex = new Regex(
                    @"<font\s+color=""(#[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?|[a-zA-Z]+)""(?:\s+weight=""bold"")?\s*>(.*?)</font>|<strong>(.*?)</strong>|([^\n<]+)",
                    RegexOptions.IgnoreCase
                );

                foreach (Match match in regex.Matches(line))
                {
                    string content = match.Groups[2].Success ? match.Groups[2].Value :
                                     match.Groups[3].Success ? match.Groups[3].Value :
                                     match.Groups[4].Success ? match.Groups[4].Value : "";

                    Vector4 color = match.Groups[1].Success ? ParseColor(match.Groups[1].Value) : Vector4.One;
                    bool bold = match.Groups[3].Success || match.Value.Contains("weight=\"bold\"");

                    string[] words = Regex.Split(content, @"(\s+)");
                    foreach (var w in words)
                    {
                        if (!string.IsNullOrEmpty(w))
                        {
                            string word = w;

                            if (isFirstWord && !string.IsNullOrWhiteSpace(word))
                            {
                                word = " " + word;
                                isFirstWord = false;
                            }

                            yield return (word, color, bold, false);
                        }
                    }
                }

                if (i < lines.Length - 1)
                    yield return (null, Vector4.One, false, true);
            }
        }

        private void DrawVTML(ImDrawListPtr drawList, ImFontPtr font, Vector2 startPos, string text, float maxWidth)
        {
            Vector2 cursor = startPos;
            float lineHeight = font.FontSize + 2f;
            float startX = cursor.X;

            bool lineStart = true;
            bool isFirstVisualLine = true;

            float spaceWidth = ImGui.CalcTextSize(" ").X;

            foreach (var (Word, Color, Bold, NewLine) in ParseVTML(text))
            {
                if (NewLine)
                {
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                    lineStart = true;
                    continue;
                }

                if (Word == null) continue;

                Vector2 size = ImGui.CalcTextSize(Word);

                if (cursor.X + size.X > startX + maxWidth)
                {
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                    lineStart = true;
                }

                if (lineStart && !isFirstVisualLine && !string.IsNullOrWhiteSpace(Word))
                {
                    cursor.X += spaceWidth;
                    lineStart = false;
                }
                else if (lineStart && isFirstVisualLine && !string.IsNullOrWhiteSpace(Word))
                {
                    lineStart = false;
                    isFirstVisualLine = false;
                }

                if (Bold)
                    drawList.AddText(font, font.FontSize, cursor + new Vector2(1, 0), ImGui.ColorConvertFloat4ToU32(Color), Word);

                drawList.AddText(font, font.FontSize, cursor, ImGui.ColorConvertFloat4ToU32(Color), Word);
                cursor.X += size.X;
            }
        }

        private Vector2 MeasureVTMLText(ImFontPtr font, string text, float maxWidth)
        {
            Vector2 cursor = new(Padding, Padding);
            float lineHeight = font.FontSize + 2f;
            float startX = cursor.X;

            bool lineStart = true;
            bool isFirstVisualLine = true;
            float spaceWidth = ImGui.CalcTextSize(" ").X;

            float maxX = startX;

            foreach (var (Word, Color, Bold, NewLine) in ParseVTML(text))
            {
                if (NewLine)
                {
                    maxX = MathF.Max(maxX, cursor.X);
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                    lineStart = true;
                    continue;
                }

                if (Word == null) continue;

                Vector2 size = ImGui.CalcTextSize(Word);

                if (cursor.X + size.X > startX + maxWidth)
                {
                    maxX = MathF.Max(maxX, cursor.X);
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                    lineStart = true;
                }

                if (lineStart && !isFirstVisualLine && !string.IsNullOrWhiteSpace(Word))
                {
                    cursor.X += spaceWidth;
                    lineStart = false;
                }
                else if (lineStart && isFirstVisualLine && !string.IsNullOrWhiteSpace(Word))
                {
                    lineStart = false;
                    isFirstVisualLine = false;
                }

                cursor.X += size.X;
            }

            maxX = MathF.Max(maxX, cursor.X);

            return new Vector2(maxX, cursor.Y + font.FontSize * 0.5f + Padding);
        }

        private Vector4 ParseColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return new Vector4(1f, 1f, 1f, 1f);

            if (color.StartsWith("#"))
            {
                if (color.Length == 7)
                {
                    uint r = uint.Parse(color.Substring(1, 2), NumberStyles.HexNumber);
                    uint g = uint.Parse(color.Substring(3, 2), NumberStyles.HexNumber);
                    uint b = uint.Parse(color.Substring(5, 2), NumberStyles.HexNumber);
                    return new Vector4(r / 255f, g / 255f, b / 255f, 1f);
                }
                else if (color.Length == 9)
                {
                    uint r = uint.Parse(color.Substring(1, 2), NumberStyles.HexNumber);
                    uint g = uint.Parse(color.Substring(3, 2), NumberStyles.HexNumber);
                    uint b = uint.Parse(color.Substring(5, 2), NumberStyles.HexNumber);
                    uint a = uint.Parse(color.Substring(7, 2), NumberStyles.HexNumber);
                    return new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
                }
            }

            try
            {
                var converter = new ColorConverter();
                var mediaColor = (Color?)converter.ConvertFromString(color) ?? Color.White;
                return new Vector4(mediaColor.R / 255f, mediaColor.G / 255f, mediaColor.B / 255f, mediaColor.A / 255f);
            }
            catch
            {
                return new Vector4(1f, 1f, 1f, 1f);
            }
        }

        private string FormatLang(string key, object[] args)
        {
            string text = Lang.GetIfExists(key) ?? key;
            if (args != null && args.Length > 0 && capi != null)
            {
                try
                {
                    text = string.Format(CultureInfo.InvariantCulture, text, args);
                }
                catch
                {
                    capi.World.Logger.Warning("toastlib: invalid format for lang key {0}", key);
                }
            }
            return text;
        }

        private class Toast
        {
            public string Text { get; }
            public float ElapsedMs = 0;
            public float CurrentY = 0;
            public float DisplayTimeMs { get; }
            public Vector4 BackgroundColor { get; }

            public Toast(string text)
            {
                Text = text ?? "";
                CurrentY = 0;
                DisplayTimeMs = toastlibModSystem.DisplayTimeMs;
                BackgroundColor = new Vector4(0, 0, 0, 0.8f);
            }

            public Toast(string text, float displayTimeMs, Vector4 backgroundColor)
            {
                Text = text ?? "";
                CurrentY = 0;
                DisplayTimeMs = displayTimeMs;
                BackgroundColor = backgroundColor;
            }
        }

        private static class MathHelper
        {
            public static float Lerp(float a, float b, float t)
            {
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
                return a + (b - a) * t;
            }
        }
    }
}