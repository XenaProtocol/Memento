
using Dalamud.Interface.Windowing;


namespace Memento.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin plugin;
        private InteractionsTab interactionsTab;
        private AdmirersTab admirersTab;
        private SettingsTab settingsTab;
        private StatsTab statsTab;

        public MainWindow(Plugin plugin) : base("Memento###MementoMain")
        {
            this.plugin = plugin;
            this.IsOpen = false;

            // This is the specific list managed by the Windowing system.
            // Based on your previous errors, we'll use the fully qualified name.
            TitleBarButtons = new List<Window.TitleBarButton>
            {
                new()
                {
                    Icon = FontAwesomeIcon.Cog,
                    // We'll use OnTrigger which is the most common version-safe name.
                    // If 'OnTrigger' still errors, change only this word to 'ClickAction' or 'Action'
                    Click = (btn) => { plugin.configWindow.IsOpen = !plugin.configWindow.IsOpen; },
                    ShowTooltip = () => ImGui.SetTooltip("Memento Visual Settings")
                }
            };

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(400, 400),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            interactionsTab = new InteractionsTab(plugin);
            admirersTab = new AdmirersTab(plugin);
            statsTab = new StatsTab(plugin);
            settingsTab = new SettingsTab(plugin);
        }

        public void Dispose() { }

        // REMOVED: DrawConfig (as it was causing the override error)

        public override void Draw()
        {
            ImGui.SetWindowFontScale(plugin.Config.FontScale);

            if (ImGui.BeginTabBar("SocialTabs"))
            {
                interactionsTab.Draw();
                admirersTab.Draw();
                statsTab.Draw();
                settingsTab.Draw();
                ImGui.EndTabBar();
            }
        }
    }
}
