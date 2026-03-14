namespace Memento.Windows
{
    // IDisposable is the magic word that lets us use a "using" block
    public class WindowCustomization : IDisposable
    {
        private int colorCount = 0;
        private int varCount = 0;

        public WindowCustomization()
        {
            // Apply all the styles the moment this class is created
            PushVar(ImGuiStyleVar.WindowRounding, 12f);
            PushVar(ImGuiStyleVar.FrameRounding, 8f);

            PushColor(ImGuiCol.WindowBg, new Vector4(1.0f, 0.92f, 0.96f, 1.0f));
            PushColor(ImGuiCol.TitleBgActive, new Vector4(1.0f, 0.6f, 0.75f, 1.0f));    // focused
            PushColor(ImGuiCol.TitleBg, new Vector4(1.0f, 0.65f, 0.8f, 1.0f));          //unfocused
            PushColor(ImGuiCol.TitleBgCollapsed, new Vector4(1.0f, 0.6f, 0.75f, 1.0f)); //minimized
            PushColor(ImGuiCol.Text, new Vector4(0.2f, 0.05f, 0.1f, 1.0f));
            PushColor(ImGuiCol.FrameBg, new Vector4(1.0f, 1.0f, 1.0f, 0.5f));
            PushColor(ImGuiCol.FrameBgHovered, new Vector4(1.0f, 1.0f, 1.0f, 0.8f));
            PushColor(ImGuiCol.FrameBgActive, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            PushColor(ImGuiCol.PopupBg, new Vector4(1.0f, 0.96f, 0.98f, 1.0f));
            PushColor(ImGuiCol.Header, new Vector4(1.0f, 0.8f, 0.9f, 1.0f));
            PushColor(ImGuiCol.HeaderHovered, new Vector4(1.0f, 0.6f, 0.75f, 1.0f));
            PushColor(ImGuiCol.Tab, new Vector4(1.0f, 0.85f, 0.92f, 1.0f));
            PushColor(ImGuiCol.TabActive, new Vector4(1.0f, 0.6f, 0.75f, 1.0f));
            PushColor(ImGuiCol.TabHovered, new Vector4(1.0f, 0.75f, 0.85f, 1.0f));
            PushColor(ImGuiCol.Button, new Vector4(1.0f, 0.6f, 0.75f, 1.0f));
            PushColor(ImGuiCol.CheckMark, new Vector4(0.9f, 0.3f, 0.5f, 1.0f));        // cute pink checkmarks
            PushColor(ImGuiCol.ScrollbarBg, new Vector4(1.0f, 0.94f, 0.97f, 0.4f));
            PushColor(ImGuiCol.ScrollbarGrab, new Vector4(0.9f, 0.3f, 0.5f, 0.8f));
            PushColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(1.0f, 0.4f, 0.6f, 1.0f));
            PushColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.8f, 0.2f, 0.4f, 1.0f));
            PushColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.4f, 0.6f, 1.0f));
            PushColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.3f, 0.5f, 1.0f)); 
            PushColor(ImGuiCol.HeaderActive, new Vector4(0.9f, 0.3f, 0.5f, 1.0f));
            PushColor(ImGuiCol.TableRowBg, new Vector4(1.0f, 1.0f, 1.0f, 0.1f));       // Very faint transparent white
            PushColor(ImGuiCol.TableRowBgAlt, new Vector4(1.0f, 0.78f, 0.88f, 0.5f)); // Soft darker pink highlight
        }

        // Our helper methods
        private void PushColor(ImGuiCol col, Vector4 vec) { ImGui.PushStyleColor(col, vec); colorCount++; }
        private void PushVar(ImGuiStyleVar styleVar, float val) { ImGui.PushStyleVar(styleVar, val); varCount++; }

        // This runs automatically at the end of the "using" block!
        public void Dispose()
        {
            ImGui.PopStyleColor(colorCount);
            ImGui.PopStyleVar(varCount);
        }
    }
}
