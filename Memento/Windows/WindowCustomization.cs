namespace Memento.Windows
{
    public class WindowCustomization : IDisposable
    {
        private int colorCount = 0;
        private int varCount = 0;

        public WindowCustomization(string themeName)
        {
            PushVar(ImGuiStyleVar.WindowRounding, 12f);
            PushVar(ImGuiStyleVar.FrameRounding, 8f);

            // Default Pink
            Vector4 bg = new Vector4(1.0f, 0.92f, 0.96f, 1.0f);
            Vector4 primary = new Vector4(0.9f, 0.4f, 0.6f, 1.0f);
            Vector4 primaryActive = new Vector4(0.9f, 0.3f, 0.5f, 1.0f);
            Vector4 textDark = new Vector4(0.3f, 0.1f, 0.2f, 1.0f);   // Deep Rose (Pink)
            Vector4 rowAlt = new Vector4(1.0f, 0.78f, 0.88f, 0.5f);
            // New Accent Color for Headers/Text
            Vector4 accent = primary;

            switch (themeName)
            {
                case "Perfectly Purple":
                    bg = new Vector4(0.95f, 0.92f, 0.98f, 1.0f);
                    primary = new Vector4(0.6f, 0.4f, 0.9f, 1.0f);
                    primaryActive = new Vector4(0.5f, 0.3f, 0.8f, 1.0f);
                    textDark = new Vector4(0.25f, 0.15f, 0.35f, 1.0f); // Deep Plum
                    rowAlt = new Vector4(0.85f, 0.78f, 1.0f, 0.5f);
                    accent = primary;
                    break;
                case "Beautifully Blue":
                    bg = new Vector4(0.92f, 0.96f, 0.98f, 1.0f);
                    primary = new Vector4(0.4f, 0.6f, 0.9f, 1.0f);
                    primaryActive = new Vector4(0.3f, 0.5f, 0.8f, 1.0f);
                    textDark = new Vector4(0.15f, 0.25f, 0.45f, 1.0f); // Deep Navy
                    rowAlt = new Vector4(0.78f, 0.88f, 1.0f, 0.5f);
                    accent = primary;
                    break;
                case "Glamourous Green":
                    bg = new Vector4(0.92f, 0.98f, 0.94f, 1.0f);
                    primary = new Vector4(0.4f, 0.8f, 0.6f, 1.0f);
                    primaryActive = new Vector4(0.3f, 0.7f, 0.5f, 1.0f);
                    textDark = new Vector4(0.1f, 0.3f, 0.2f, 1.0f);   // Deep Forest
                    rowAlt = new Vector4(0.78f, 1.0f, 0.88f, 0.5f);
                    accent = primary;
                    break;
            }

            uiThemeAssigner(bg, primary, primaryActive, textDark, rowAlt);
        }

        private void uiThemeAssigner(Vector4 bg, Vector4 primary, Vector4 primaryActive, Vector4 textDark, Vector4 rowAlt)
        {
            PushColor(ImGuiCol.WindowBg, bg);
            PushColor(ImGuiCol.PopupBg, bg);
            PushColor(ImGuiCol.Text, textDark);

            // Ensure Title Bars stay themed
            PushColor(ImGuiCol.TitleBg, primary);
            PushColor(ImGuiCol.TitleBgActive, primary);
            PushColor(ImGuiCol.TitleBgCollapsed, primary);

            //Headers
            PushColor(ImGuiCol.Header, primary);
            PushColor(ImGuiCol.HeaderHovered, primaryActive);
            PushColor(ImGuiCol.HeaderActive, primaryActive);



            // Tabs and Buttons
            PushColor(ImGuiCol.Tab, primary);
            PushColor(ImGuiCol.TabActive, primaryActive);
            PushColor(ImGuiCol.TabHovered, primaryActive);
            PushColor(ImGuiCol.Button, primary);
            PushColor(ImGuiCol.ButtonHovered, primaryActive);
            PushColor(ImGuiCol.ButtonActive, primaryActive);

            PushColor(ImGuiCol.CheckMark, primary);
            // Frames
            PushColor(ImGuiCol.FrameBg, new Vector4(1.0f, 1.0f, 1.0f, 0.5f));
            PushColor(ImGuiCol.FrameBgHovered, new Vector4(1.0f, 1.0f, 1.0f, 0.8f));
            PushColor(ImGuiCol.FrameBgActive, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            PushColor(ImGuiCol.TableRowBgAlt, rowAlt);
        }

        private void PushColor(ImGuiCol col, Vector4 vec) { ImGui.PushStyleColor(col, vec); colorCount++; }
        private void PushVar(ImGuiStyleVar styleVar, float val) { ImGui.PushStyleVar(styleVar, val); varCount++; }

        public void Dispose()
        {
            ImGui.PopStyleColor(colorCount);
            ImGui.PopStyleVar(varCount);
        }
    }
}
