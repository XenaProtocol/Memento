using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using EnzirionCore.UI;
using System.Collections.Generic;
using System.Numerics;

namespace Memento.Windows
{
    /// <summary>
    /// Primary Memento window — hosts all tabs (Pet, Social, Journal, Help, Settings, Dev).
    /// Title bar includes paw (popout) and gear (config) buttons.
    /// Shows a login gate when no character is loaded.
    ///
    /// Now inherits StandardWindow — theme push/pop handled automatically
    /// in PreDraw/PostDraw. WindowCustomization is no longer needed.
    /// </summary>
    public class MainWindow : StandardWindow
    {
        private Plugin plugin;
        private readonly List<IDrawablePage> _tabs;
        private readonly Tabs.DevTab devTab;

        public MainWindow(Plugin plugin) : base("Memento  ✦###MementoMain")
        {
            this.plugin = plugin;
            this.IsOpen = false;

            TitleBarButtons = new List<Window.TitleBarButton>
            {
                new() { Icon = FontAwesomeIcon.Paw,
                    Click = _ => plugin.petPopout.IsOpen = !plugin.petPopout.IsOpen,
                    ShowTooltip = () => ImGui.SetTooltip("Toggle Pet Popout  (/mementopet)") },
                new() { Icon = FontAwesomeIcon.Cog,
                    Click = _ => plugin.configWindow.IsOpen = !plugin.configWindow.IsOpen,
                    ShowTooltip = () => ImGui.SetTooltip("Visual Settings") },
            };

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(460, 480),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            };
            Size = new Vector2(700, 640);
            SizeCondition = ImGuiCond.FirstUseEver;

            _tabs = new List<IDrawablePage>
            {
                new Tabs.PetTab(plugin),
                new Tabs.SocialTab(plugin),
                new Tabs.JournalTab(plugin),
                new Tabs.HelpTab(plugin),
                new Tabs.SettingsTab(plugin),
            };
            devTab = new Tabs.DevTab(plugin);
        }

        // ── StandardWindow requires this — returns the active palette each frame ──
        protected override ThemePalette GetCurrentPalette() =>
            ThemeRegistry.GetByLegacyName(plugin.Config.Theme) ?? ThemeRegistry.PerfectlyPurple;

        // NOTE: PreDraw/PostDraw are handled by StandardWindow.
        // The full ~40 ImGui colour push + 8 style vars happen automatically.
        // WindowCustomization class is no longer needed.

        public override void Draw()
        {
            // Gate: require a logged-in character before showing any content
            if (Plugin.ObjectTable.LocalPlayer == null)
            {
                ImGui.SetWindowFontScale(plugin.Config.FontScale);
                float W = ImGui.GetContentRegionAvail().X;
                float H = ImGui.GetContentRegionAvail().Y;
                string msg = "Waiting for character data...";
                var sz = ImGui.CalcTextSize(msg);
                ImGui.SetCursorPos(new Vector2((W - sz.X) / 2f, H / 2f - sz.Y));
                ImGui.TextColored(MementoColors.Dim, msg);
                ImGui.SetWindowFontScale(1.0f);
                return;
            }

            ImGui.SetWindowFontScale(plugin.Config.FontScale);
            var t = GetCurrentPalette();
            FolderTabBar.PushStyle(t);
            if (ImGui.BeginTabBar("MementoTabs", ImGuiTabBarFlags.FittingPolicyResizeDown))
            {
                foreach (var tab in _tabs)
                    FolderTabBar.Tab(tab.TabLabel, tab.Draw);
                if (plugin.Config.DevUnlocked)
                    FolderTabBar.Tab(devTab.TabLabel, devTab.Draw);
                ImGui.EndTabBar();
            }
            FolderTabBar.PopStyle();
            ImGui.SetWindowFontScale(1.0f);
        }
    }
}
