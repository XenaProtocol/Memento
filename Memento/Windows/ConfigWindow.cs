using Dalamud.Interface.Windowing;

namespace Memento.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private Plugin plugin;
        private string[] themes = { "Pretty in Pink", "Perfectly Purple", "Glamourous Green", "Beautifully Blue" };

        public ConfigWindow(Plugin plugin) : base("Memento Visual Settings###MementoConfig")
        {
            this.plugin = plugin;

            // Makes the window look like a proper settings panel
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        }

        public void Dispose() { }

        public override void Draw()
        {
            using (var theme = new WindowCustomization(plugin.Config.Theme))
            {
                ImGui.TextColored(plugin.GetThemeColor(), "Visual Settings");
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text("UI Scale:");
                float scale = plugin.Config.FontScale;
                if (ImGui.SliderFloat("##FontScale", ref scale, 0.7f, 1.5f, "%.2fx"))
                {
                    plugin.Config.FontScale = scale;
                    plugin.Config.Save();
                }

                ImGui.Spacing();

                ImGui.Text("Color Theme:");
                if (ImGui.BeginCombo("##ThemeSelect", plugin.Config.Theme))
                {
                    foreach (var t in themes)
                    {
                        if (ImGui.Selectable(t, t == plugin.Config.Theme))
                        {
                            plugin.Config.Theme = t;
                            plugin.Config.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
            }
        }
    }
}
