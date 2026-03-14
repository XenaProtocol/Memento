namespace Memento.Windows.Tabs
{
    public class SettingsTab
    {
        private Plugin plugin;

        // These now live here instead of the main window!
        private string emoteSearch = "";
        private uint selectedEmoteId = 0;

        public SettingsTab(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Settings"))
            {
                var themeColor = plugin.GetThemeColor(); // Grab the helper!

                // Chat Settings
                ImGui.TextColored(themeColor, "Chat Notifications:");
                chatNoti();

                ImGui.Separator();
                ImGui.Spacing();

                // Emote Tracker Settings
                ImGui.TextColored(themeColor, "Add a new emote:");
                emoteTracker();

                ImGui.Spacing();
                ImGui.TextColored(themeColor, "Currently Admiring:");

                // Tracked Emotes List
                trackedEmotes();

                ImGui.EndTabItem();
            }
        }

        private void trackedEmotes()
        {
            if (ImGui.BeginChild("FilterList", new Vector2(0, 0), true))
            {
                // alternating zebra stripes!
                if (ImGui.BeginTable("EmoteTable", 2, ImGuiTableFlags.RowBg))
                {
                    // Column 1 stretches to fill space, Column 2 stays exactly 30 pixels wide for the button
                    ImGui.TableSetupColumn("EmoteName", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("DeleteBtn", ImGuiTableColumnFlags.WidthFixed, 30f);

                    string toRemove = null;
                    foreach (var emoteName in plugin.Config.TrackedEmotes)
                    {
                        ImGui.TableNextRow(); // Moves down to the next zebra stripe

                        // Column 1: The Emote Name
                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding(); // Keeps the text vertically centered with the button!
                        ImGui.Text($"✨ {emoteName}");

                        // Column 2: The Delete Button
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Button, plugin.GetThemeColor());
                        if (ImGui.Button($"X##{emoteName}")) toRemove = emoteName;
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndTable();

                    // Safely remove the item after the table finishes drawing
                    if (toRemove != null)
                    {
                        plugin.Config.TrackedEmotes.Remove(toRemove);
                        plugin.Config.Save();
                    }
                }
                ImGui.EndChild();
            }
        }

        private void chatNoti()
        {
            bool showEmoteChat = plugin.Config.ShowEmoteChat;
            if (ImGui.Checkbox("Print emotes to chat box", ref showEmoteChat))
            {
                plugin.Config.ShowEmoteChat = showEmoteChat;
                plugin.Config.Save();
            }

            bool showTargetChat = plugin.Config.ShowTargetChat;
            if (ImGui.Checkbox("Print when someone targets me", ref showTargetChat))
            {
                plugin.Config.ShowTargetChat = showTargetChat;
                plugin.Config.Save();
            }
        }

        private void emoteTracker()
        {
            // --- Emote Tracker Settings ---
            // 1. Figure out what the dropdown should say BEFORE we draw it
            string comboPreview = "Select an emote...";
            if (selectedEmoteId != 0)
            {
                var selectedRow = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>()?.GetRow(selectedEmoteId);
                if (selectedRow.HasValue && !string.IsNullOrEmpty(selectedRow.Value.Name.ToString()))
                {
                    comboPreview = $"{selectedRow.Value.Name}         (ID: {selectedEmoteId})";
                }
            }

            // 2. Draw the combo box with our new fancy title
            if (ImGui.BeginCombo("##EmoteSelector", comboPreview))
            {
                // "Search:" text on the left
                ImGui.Text("Search:");
                ImGui.SameLine();

                // Temporarily make the search bar background match the theme, but slightly darker!
                ImGui.PushStyleColor(ImGuiCol.FrameBg, plugin.GetSearchBgColor());

                // Use InputTextWithHint for that cool ghost-text placeholder
                ImGui.InputTextWithHint("##InsideSearch", "type here to search...", ref emoteSearch, 64);

                ImGui.PopStyleColor(); // Pop the dark color so the rest of the UI stays light

                ImGui.Separator();

                var emotes = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>();
                foreach (var emote in emotes)
                {
                    string name = emote.Name.ToString();
                    if (string.IsNullOrEmpty(name)) continue;

                    if (name.Contains(emoteSearch, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ImGui.Selectable($"{name}##{emote.RowId}")) selectedEmoteId = emote.RowId;
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("Add") && selectedEmoteId != 0)
            {
                var row = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>()?.GetRow(selectedEmoteId);
                if (row.HasValue && !string.IsNullOrEmpty(row.Value.Name.ToString()))
                {
                    if (!plugin.Config.TrackedEmotes.Contains(row.Value.Name.ToString()))
                    {
                        plugin.Config.TrackedEmotes.Add(row.Value.Name.ToString());

                        emoteSearch = "";
                        selectedEmoteId = 0;

                        plugin.Config.Save();
                    }
                }
            }
        }
    }
}
