namespace Memento.Windows.Tabs
{
    public class AdmirersTab
    {
        private Plugin plugin;

        public AdmirersTab(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Admirers"))
            {
                // Use the theme helper for these status messages!
                var themeColor = plugin.GetThemeColor();

                if (plugin.currentlyTargetingMe.Count == 0)
                {
                    ImGui.TextColored(themeColor, "Just you and your cute glam for now!");
                }
                else
                {
                    ImGui.TextColored(themeColor, $"Currently being admired by: {plugin.currentlyTargetingMe.Count}");
                }

                ImGui.Separator();
                if (ImGui.BeginChild("TargetLog", new Vector2(0, -35), true))
                {
                    if (plugin.Config.TargetHistory.Count == 0) ImGui.TextDisabled("Just you and your cute glam for now!");
                    foreach (var t in plugin.Config.TargetHistory) ImGui.TextWrapped(t);
                    ImGui.EndChild();
                }
                if (ImGui.Button("Clear History")) { plugin.Config.TargetHistory.Clear(); plugin.Config.Save(); }
                ImGui.EndTabItem();
            }
        }
    }
}
