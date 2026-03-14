namespace Memento.Windows
{
    public class MainWindow
    {
        private Plugin plugin;
        public bool IsVisible = false;

        // Our individual tab classes4
        private InteractionsTab interactionsTab;
        private AdmirersTab admirersTab;
        private SettingsTab settingsTab;
        private StatsTab statsTab;

        public MainWindow(Plugin plugin)
        {
            this.plugin = plugin;

            // Initialize the tabs when the window is created
            interactionsTab = new InteractionsTab(plugin);
            admirersTab = new AdmirersTab(plugin);
            statsTab = new StatsTab(plugin);
            settingsTab = new SettingsTab(plugin);
        }

        public void DrawUI()
        {
            if (!IsVisible) return;
            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(400, 400), new Vector2(float.MaxValue, float.MaxValue));

            using (var theme = new WindowCustomization())
            {
                if (ImGui.Begin("Memento", ref IsVisible))
                {
                    if (ImGui.BeginTabBar("SocialTabs"))
                    {
                        // Simply tell each tab to draw itself!
                        interactionsTab.Draw();
                        admirersTab.Draw();
                        statsTab.Draw();
                        settingsTab.Draw();
                        ImGui.EndTabBar();
                    }
                }
                ImGui.End();
            }
        }
    }
}
