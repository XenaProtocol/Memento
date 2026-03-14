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
                if (plugin.currentlyTargetingMe.Count == 0)
                {
                    ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), $"Just you and your cute glam for now!");
                }
                if (plugin.currentlyTargetingMe.Count >= 1)
                {
                    ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), $"Currently being admired by: {plugin.currentlyTargetingMe.Count}");
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
