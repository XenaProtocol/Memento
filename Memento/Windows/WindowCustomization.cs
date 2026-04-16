using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Memento.Windows
{
    public class WindowCustomization : IDisposable
    {
        private int colorCount = 0;
        private int varCount   = 0;

        public WindowCustomization(string themeName)
        {
            PushVar(ImGuiStyleVar.WindowRounding,  12f);
            PushVar(ImGuiStyleVar.FrameRounding,    7f);
            PushVar(ImGuiStyleVar.TabRounding,       6f);
            PushVar(ImGuiStyleVar.ScrollbarRounding, 6f);
            PushVar(ImGuiStyleVar.GrabRounding,      6f);
            PushVar(ImGuiStyleVar.WindowPadding,  new Vector2(12f, 12f));
            PushVar(ImGuiStyleVar.FramePadding,   new Vector2(8f,  5f));
            PushVar(ImGuiStyleVar.ItemSpacing,    new Vector2(8f,  6f));

            switch (themeName)
            {
                case "Perfectly Purple":         PurpleNight(); break;
                case "Perfectly Purple (Day)":   PurpleDay();   break;
                case "Pretty in Pink":           PinkNight();   break;
                case "Pretty in Pink (Day)":     PinkDay();     break;
                case "Glamourous Green":         GreenNight();  break;
                case "Glamourous Green (Day)":   GreenDay();    break;
                case "Beautifully Blue":         BlueNight();   break;
                case "Beautifully Blue (Day)":   BlueDay();     break;
                default:                         PurpleNight(); break;
            }
        }

        // ================================================================
        // PURPLE
        // ================================================================
        private void PurpleNight() => Dark(
            bg:       V(0.10f, 0.06f, 0.20f),
            panel:    V(0.18f, 0.10f, 0.35f),
            title:    V(0.14f, 0.08f, 0.27f),
            accent:   V(0.55f, 0.36f, 0.96f),
            accentH:  V(0.65f, 0.48f, 1.00f),
            accentA:  V(0.44f, 0.27f, 0.80f),
            text:     V(0.94f, 0.91f, 1.00f),
            textDim:  V(0.61f, 0.50f, 0.83f),
            border:   V(0.36f, 0.22f, 0.66f, 0.50f)
        );

        private void PurpleDay() => Light(
            bg:       V(0.94f, 0.91f, 0.98f),
            panel:    V(0.86f, 0.80f, 0.95f),
            title:    V(0.44f, 0.26f, 0.78f),
            accent:   V(0.44f, 0.26f, 0.82f),
            accentH:  V(0.34f, 0.18f, 0.70f),
            accentA:  V(0.28f, 0.14f, 0.60f),
            text:     V(0.18f, 0.09f, 0.32f),
            textDim:  V(0.40f, 0.28f, 0.58f),
            border:   V(0.55f, 0.40f, 0.85f, 0.45f)
        );

        // ================================================================
        // PINK
        // ================================================================
        private void PinkNight() => Dark(
            bg:       V(0.13f, 0.05f, 0.09f),
            panel:    V(0.22f, 0.08f, 0.15f),
            title:    V(0.18f, 0.06f, 0.12f),
            accent:   V(0.98f, 0.25f, 0.58f),
            accentH:  V(1.00f, 0.42f, 0.68f),
            accentA:  V(0.82f, 0.16f, 0.44f),
            text:     V(1.00f, 0.93f, 0.96f),
            textDim:  V(0.82f, 0.58f, 0.70f),
            border:   V(0.82f, 0.22f, 0.48f, 0.45f)
        );

        private void PinkDay() => Light(
            bg:       V(1.00f, 0.93f, 0.96f),
            panel:    V(0.99f, 0.84f, 0.90f),
            title:    V(0.88f, 0.20f, 0.50f),
            accent:   V(0.88f, 0.18f, 0.48f),
            accentH:  V(0.72f, 0.10f, 0.36f),
            accentA:  V(0.60f, 0.06f, 0.28f),
            text:     V(0.28f, 0.06f, 0.14f),
            textDim:  V(0.55f, 0.22f, 0.36f),
            border:   V(0.92f, 0.55f, 0.70f, 0.55f)
        );

        // ================================================================
        // GREEN
        // ================================================================
        private void GreenNight() => Dark(
            bg:       V(0.04f, 0.12f, 0.08f),
            panel:    V(0.07f, 0.20f, 0.13f),
            title:    V(0.04f, 0.15f, 0.10f),
            accent:   V(0.22f, 0.82f, 0.50f),
            accentH:  V(0.30f, 0.94f, 0.60f),
            accentA:  V(0.14f, 0.64f, 0.36f),
            text:     V(0.88f, 1.00f, 0.92f),
            textDim:  V(0.48f, 0.76f, 0.58f),
            border:   V(0.20f, 0.64f, 0.38f, 0.45f)
        );

        private void GreenDay() => Light(
            bg:       V(0.91f, 0.98f, 0.93f),
            panel:    V(0.80f, 0.94f, 0.84f),
            title:    V(0.10f, 0.58f, 0.30f),
            accent:   V(0.10f, 0.55f, 0.28f),
            accentH:  V(0.06f, 0.44f, 0.22f),
            accentA:  V(0.04f, 0.34f, 0.16f),
            text:     V(0.04f, 0.18f, 0.09f),
            textDim:  V(0.16f, 0.42f, 0.24f),
            border:   V(0.22f, 0.72f, 0.42f, 0.45f)
        );

        // ================================================================
        // BLUE
        // ================================================================
        private void BlueNight() => Dark(
            bg:       V(0.04f, 0.08f, 0.18f),
            panel:    V(0.08f, 0.14f, 0.30f),
            title:    V(0.05f, 0.10f, 0.22f),
            accent:   V(0.28f, 0.60f, 1.00f),
            accentH:  V(0.40f, 0.72f, 1.00f),
            accentA:  V(0.18f, 0.46f, 0.86f),
            text:     V(0.90f, 0.94f, 1.00f),
            textDim:  V(0.50f, 0.65f, 0.88f),
            border:   V(0.26f, 0.48f, 0.86f, 0.45f)
        );

        private void BlueDay() => Light(
            bg:       V(0.91f, 0.95f, 1.00f),
            panel:    V(0.80f, 0.88f, 0.99f),
            title:    V(0.16f, 0.42f, 0.88f),
            accent:   V(0.14f, 0.40f, 0.86f),
            accentH:  V(0.08f, 0.30f, 0.72f),
            accentA:  V(0.05f, 0.22f, 0.58f),
            text:     V(0.04f, 0.10f, 0.26f),
            textDim:  V(0.20f, 0.35f, 0.60f),
            border:   V(0.30f, 0.58f, 0.94f, 0.45f)
        );

        // ================================================================
        // Dark theme applier — light text on dark bg
        // ================================================================
        private void Dark(
            Vector4 bg, Vector4 panel, Vector4 title,
            Vector4 accent, Vector4 accentH, Vector4 accentA,
            Vector4 text, Vector4 textDim, Vector4 border)
        {
            var frameBg    = Lerp(bg, panel, 0.6f);
            var frameBgHov = Lerp(bg, panel, 0.9f);
            var frameBgAct = panel;

            PushColor(ImGuiCol.WindowBg,             bg);
            PushColor(ImGuiCol.ChildBg,              Lerp(bg, panel, 0.3f));
            PushColor(ImGuiCol.PopupBg,              panel);
            PushColor(ImGuiCol.Text,                 text);
            PushColor(ImGuiCol.TextDisabled,         textDim with { W = 0.55f });
            PushColor(ImGuiCol.Border,               border);
            PushColor(ImGuiCol.BorderShadow,         V(0, 0, 0, 0));
            PushColor(ImGuiCol.FrameBg,              frameBg);
            PushColor(ImGuiCol.FrameBgHovered,       frameBgHov);
            PushColor(ImGuiCol.FrameBgActive,        frameBgAct);
            PushColor(ImGuiCol.TitleBg,              title);
            PushColor(ImGuiCol.TitleBgActive,        title);
            PushColor(ImGuiCol.TitleBgCollapsed,     title);
            PushColor(ImGuiCol.MenuBarBg,            bg);
            PushColor(ImGuiCol.ScrollbarBg,          bg with { W = 0.4f });
            PushColor(ImGuiCol.ScrollbarGrab,        accent with { W = 0.4f });
            PushColor(ImGuiCol.ScrollbarGrabHovered, accent with { W = 0.65f });
            PushColor(ImGuiCol.ScrollbarGrabActive,  accent);
            PushColor(ImGuiCol.CheckMark,            accent);
            PushColor(ImGuiCol.SliderGrab,           accent);
            PushColor(ImGuiCol.SliderGrabActive,     accentH);
            PushColor(ImGuiCol.Button,               accent  with { W = 0.22f });
            PushColor(ImGuiCol.ButtonHovered,        accentH with { W = 0.38f });
            PushColor(ImGuiCol.ButtonActive,         accentA with { W = 0.55f });
            PushColor(ImGuiCol.Header,               accent  with { W = 0.25f });
            PushColor(ImGuiCol.HeaderHovered,        accentH with { W = 0.40f });
            PushColor(ImGuiCol.HeaderActive,         accentA with { W = 0.55f });
            PushColor(ImGuiCol.Separator,            border);
            PushColor(ImGuiCol.SeparatorHovered,     accent with { W = 0.6f });
            PushColor(ImGuiCol.SeparatorActive,      accent);
            PushColor(ImGuiCol.ResizeGrip,           accent  with { W = 0.18f });
            PushColor(ImGuiCol.ResizeGripHovered,    accentH with { W = 0.38f });
            PushColor(ImGuiCol.ResizeGripActive,     accentA with { W = 0.55f });
            PushColor(ImGuiCol.Tab,                  accent  with { W = 0.22f });
            PushColor(ImGuiCol.TabHovered,           accentH with { W = 0.50f });
            PushColor(ImGuiCol.TabActive,            accent  with { W = 0.55f });
            PushColor(ImGuiCol.TabUnfocused,         accent  with { W = 0.10f });
            PushColor(ImGuiCol.TabUnfocusedActive,   accent  with { W = 0.30f });
            PushColor(ImGuiCol.TableRowBgAlt,        V(1f, 1f, 1f, 0.025f));
            PushColor(ImGuiCol.TableBorderLight,     border);
            PushColor(ImGuiCol.TableBorderStrong,    border with { W = border.W + 0.3f });
        }

        // ================================================================
        // Light theme applier — dark text on light bg, accent is DARK
        // ================================================================
        private void Light(
            Vector4 bg, Vector4 panel, Vector4 title,
            Vector4 accent, Vector4 accentH, Vector4 accentA,
            Vector4 text, Vector4 textDim, Vector4 border)
        {
            // For light themes, frame backgrounds are white-ish
            var frameBg    = V(1f, 1f, 1f, 0.60f);
            var frameBgHov = V(1f, 1f, 1f, 0.85f);
            var frameBgAct = V(1f, 1f, 1f, 1.00f);

            PushColor(ImGuiCol.WindowBg,             bg);
            PushColor(ImGuiCol.ChildBg,              panel);
            PushColor(ImGuiCol.PopupBg,              bg);
            PushColor(ImGuiCol.Text,                 text);
            PushColor(ImGuiCol.TextDisabled,         textDim with { W = 0.55f });
            PushColor(ImGuiCol.Border,               border);
            PushColor(ImGuiCol.BorderShadow,         V(0, 0, 0, 0));
            PushColor(ImGuiCol.FrameBg,              frameBg);
            PushColor(ImGuiCol.FrameBgHovered,       frameBgHov);
            PushColor(ImGuiCol.FrameBgActive,        frameBgAct);
            PushColor(ImGuiCol.TitleBg,              title);
            PushColor(ImGuiCol.TitleBgActive,        title);
            PushColor(ImGuiCol.TitleBgCollapsed,     title);
            PushColor(ImGuiCol.MenuBarBg,            bg);
            PushColor(ImGuiCol.ScrollbarBg,          panel with { W = 0.6f });
            PushColor(ImGuiCol.ScrollbarGrab,        accent with { W = 0.5f });
            PushColor(ImGuiCol.ScrollbarGrabHovered, accent with { W = 0.75f });
            PushColor(ImGuiCol.ScrollbarGrabActive,  accentH);
            PushColor(ImGuiCol.CheckMark,            accent);
            PushColor(ImGuiCol.SliderGrab,           accent);
            PushColor(ImGuiCol.SliderGrabActive,     accentH);
            // Buttons: darker tinted fill so they're visible on light bg
            PushColor(ImGuiCol.Button,               accent  with { W = 0.18f });
            PushColor(ImGuiCol.ButtonHovered,        accentH with { W = 0.30f });
            PushColor(ImGuiCol.ButtonActive,         accentA with { W = 0.45f });
            PushColor(ImGuiCol.Header,               accent  with { W = 0.18f });
            PushColor(ImGuiCol.HeaderHovered,        accentH with { W = 0.28f });
            PushColor(ImGuiCol.HeaderActive,         accentA with { W = 0.40f });
            PushColor(ImGuiCol.Separator,            border);
            PushColor(ImGuiCol.SeparatorHovered,     accent with { W = 0.55f });
            PushColor(ImGuiCol.SeparatorActive,      accent);
            PushColor(ImGuiCol.ResizeGrip,           accent  with { W = 0.18f });
            PushColor(ImGuiCol.ResizeGripHovered,    accentH with { W = 0.35f });
            PushColor(ImGuiCol.ResizeGripActive,     accentA with { W = 0.50f });
            // Tabs: solid accent background so they read clearly on light bg
            PushColor(ImGuiCol.Tab,                  accent  with { W = 0.16f });
            PushColor(ImGuiCol.TabHovered,           accentH with { W = 0.30f });
            PushColor(ImGuiCol.TabActive,            accent  with { W = 0.35f });
            PushColor(ImGuiCol.TabUnfocused,         accent  with { W = 0.08f });
            PushColor(ImGuiCol.TabUnfocusedActive,   accent  with { W = 0.22f });
            PushColor(ImGuiCol.TableRowBgAlt,        V(0f, 0f, 0f, 0.03f));
            PushColor(ImGuiCol.TableBorderLight,     border);
            PushColor(ImGuiCol.TableBorderStrong,    border with { W = border.W + 0.3f });
        }

        // ================================================================
        // Helpers
        // ================================================================
        private static Vector4 V(float r, float g, float b, float a = 1f) => new(r, g, b, a);

        private static Vector4 Lerp(Vector4 a, Vector4 b, float t) => new(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t
        );

        private void PushColor(ImGuiCol col, Vector4 vec) { ImGui.PushStyleColor(col, vec); colorCount++; }
        private void PushVar(ImGuiStyleVar v, float f)    { ImGui.PushStyleVar(v, f); varCount++; }
        private void PushVar(ImGuiStyleVar v, Vector2 v2) { ImGui.PushStyleVar(v, v2); varCount++; }

        public void Dispose()
        {
            ImGui.PopStyleColor(colorCount);
            ImGui.PopStyleVar(varCount);
        }
    }
}
