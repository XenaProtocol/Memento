namespace Memento.Windows.Tabs
{
    public class InteractionsTab
    {
        private Plugin plugin;

        public InteractionsTab(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Interactions"))
            {
                // Grab the dynamic theme color
                ImGui.TextColored(plugin.GetThemeColor(), "Recent Sweet Moments:");

                ImGui.Separator();
                if (ImGui.BeginChild("EmoteLog", new Vector2(0, -35), true))
                {
                    if (plugin.Config.SocialLog.Count == 0) ImGui.TextDisabled("Waiting for someone to be sweet to you...");
                    foreach (var log in plugin.Config.SocialLog) ImGui.TextWrapped(log);
                    ImGui.EndChild();
                }
                if (ImGui.Button("Clear History")) { plugin.Config.SocialLog.Clear(); plugin.Config.Save(); }
                ImGui.EndTabItem();
            }
        }
    }
}
