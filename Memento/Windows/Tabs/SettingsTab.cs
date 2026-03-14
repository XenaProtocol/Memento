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
                // Chat Settings
                ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), "Chat Notifications:");

                chatNoti();

                ImGui.Separator();
                ImGui.Spacing();

                // Emote Tracker Settings
                emoteTracker();

                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), "Currently Admiring:");

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
                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.9f, 0.4f, 0.5f, 1.0f));
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
            ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), "Add a new emote:");

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

                // Temporarily make the search bar background a bit darker!
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.9f, 0.8f, 0.85f, 1.0f));

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
