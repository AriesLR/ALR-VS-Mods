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
        private const float ToastHeight = 15f;
        private const float SlideTimeMs = 500f;
        private const float DisplayTimeMs = 5000f;
        private const float Padding = 6f;

        public void ShowToast(string langKey, params object[] args)
        {
            if (capi == null) return;
            string text = FormatLang(langKey, args);
            toastQueue.Add(new Toast(text));
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
                .WithDescription("Show example toast")
                .HandleWith(OnExampleCmd);

            root.BeginSubCommand("wrap")
                .WithDescription("Show word wrap toast")
                .HandleWith(OnWrapCmd);
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

            for (int i = toastQueue.Count - 1; i >= 0; i--)
            {
                var t = toastQueue[i];
                t.ElapsedMs += deltaTime * 1000f;

                float targetX = 10f;
                float startX = -ToastWidth;
                float endX = -ToastWidth;

                float x = t.ElapsedMs < SlideTimeMs
                    ? MathHelper.Lerp(startX, targetX, t.ElapsedMs / SlideTimeMs)
                    : t.ElapsedMs > DisplayTimeMs
                        ? MathHelper.Lerp(targetX, endX, (t.ElapsedMs - DisplayTimeMs) / SlideTimeMs)
                        : targetX;

                Vector2 textSize = MeasureVTMLText(font, t.Text, ToastWidth - 2 * Padding);
                float toastHeight = Math.Max(ToastHeight, textSize.Y + 2 * Padding);

                float y = 10f + i * (toastHeight + 10f);
                Vector2 pos = new(x, y);
                Vector2 size = new(ToastWidth, toastHeight);

                drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.8f)), 4f);
                DrawVTML(drawList, font, pos + new Vector2(Padding, Padding), t.Text, ToastWidth - 2 * Padding);

                if (t.ElapsedMs > DisplayTimeMs + SlideTimeMs)
                    toastQueue.RemoveAt(i);
            }
        }

        private IEnumerable<(string? Word, Vector4 Color, bool Bold, bool NewLine)> ParseVTML(string text)
        {
            string[] lines = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase).Split('\n');
            bool isFirstWord = true;

            foreach (var line in lines)
            {
                var regex = new Regex(@"<font\s+color=""(#[0-9a-fA-F]{6}|[a-zA-Z]+)""(?:\s+weight=""bold"")?\s*>(.*?)</font>|<strong>(.*?)</strong>|([^\n<]+)", RegexOptions.IgnoreCase);

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

                yield return (null, Vector4.One, false, true);
            }
        }

        private void DrawVTML(ImDrawListPtr drawList, ImFontPtr font, Vector2 startPos, string text, float maxWidth)
        {
            Vector2 cursor = startPos;
            float lineHeight = font.FontSize + 2f;
            float startX = cursor.X;

            foreach (var (Word, Color, Bold, NewLine) in ParseVTML(text))
            {
                if (NewLine)
                {
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                    continue;
                }

                if (Word == null) continue;

                Vector2 size = ImGui.CalcTextSize(Word);

                if (cursor.X + size.X > startX + maxWidth)
                {
                    cursor.X = startX;
                    cursor.Y += lineHeight;
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

            foreach (var (Word, Color, Bold, NewLine) in ParseVTML(text))
            {
                if (NewLine)
                {
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                    continue;
                }

                if (Word == null) continue;

                Vector2 size = ImGui.CalcTextSize(Word);
                if (cursor.X + size.X > startX + maxWidth)
                {
                    cursor.X = startX;
                    cursor.Y += lineHeight;
                }

                cursor.X += size.X;
            }

            return cursor;
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

            public Toast(string text)
            {
                Text = text ?? "";
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