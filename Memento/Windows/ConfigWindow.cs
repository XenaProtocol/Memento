using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Memento.Windows
{
    public class ConfigWindow : StandardWindow
    {
        protected override ThemePalette GetCurrentPalette() =>
            ThemeRegistry.GetByLegacyName(plugin.Config.Theme) ?? ThemeRegistry.PerfectlyPurple;

        private Plugin plugin;
        private static readonly string[] Themes = {
            "Perfectly Purple", "Pretty in Pink", "Glamourous Green", "Beautifully Blue"
        };

        private static Vector4 Purple5 = new(0.65f, 0.55f, 0.98f, 1f);
        private static Vector4 TextDim = new(0.61f, 0.50f, 0.83f, 1f);
        private static Vector4 TextPri = new(0.94f, 0.91f, 1.00f, 1f);

        public ConfigWindow(Plugin plugin) : base("Memento Visual Settings###MementoConfig")
        {
            this.plugin = plugin;
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize;
        }

        public override void Dispose() { }

        public override void Draw()
        {
            ImGui.TextColored(Purple5, "Visual Settings");
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(TextDim, "UI SCALE");
            float scale = plugin.Config.FontScale;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat("##FontScale", ref scale, 0.7f, 1.5f, "%.2fx"))
            {
                plugin.Config.FontScale = scale;
                plugin.Config.Save();
            }

            ImGui.Spacing();
            ImGui.TextColored(TextDim, "COLOR THEME");
            ImGui.SetNextItemWidth(200f);
            if (ImGui.BeginCombo("##ThemeSelect", plugin.Config.Theme))
            {
                foreach (var t in Themes)
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
