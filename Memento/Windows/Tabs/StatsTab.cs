namespace Memento.Windows.Tabs
{
    public class StatsTab
    {
        private Plugin plugin;

        public StatsTab(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Stats"))
            {
                paparazzi();

                ImGui.Spacing();
                ImGui.Spacing();

                popularEmotes();

                ImGui.Spacing();
                ImGui.Spacing();
                trackedEmotes();

                footer();

                ImGui.EndTabItem();
            }
        }

        private void footer()
        {
            if (ImGui.Button("Reset All Stats"))
            {
                plugin.Config.EmoteCounts.Clear();
                plugin.Config.AdmirerCounts.Clear();
                plugin.Config.TargetCounts.Clear();
                plugin.Config.Save();
            }

            int totalInteractions = plugin.Config.EmoteCounts.Values.Sum();
            string totalText = $"Total Admirations: {totalInteractions}";

            float posX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(totalText).X;
            ImGui.SameLine(posX);

            // Replaced hardcoded pink/red with theme color
            ImGui.TextColored(plugin.GetThemeColor(), totalText);
        }

        private void trackedEmotes()
        {
            ImGui.TextColored(plugin.GetThemeColor(), "🎯 Tracked Emote Totals 🎯");
            ImGui.Separator();

            if (ImGui.BeginChild("TrackedTotals", new Vector2(0, -35), true))
            {
                if (ImGui.BeginTable("StatsTable", 2, ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("EmoteName", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 40f);

                    foreach (var tracked in plugin.Config.TrackedEmotes)
                    {
                        int count = plugin.Config.EmoteCounts.GetValueOrDefault(tracked);
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text($"{tracked}:");

                        ImGui.TableNextColumn();
                        // Replaced hardcoded count color
                        ImGui.TextColored(plugin.GetThemeColor(), count.ToString());
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
        }

        private void popularEmotes()
        {
            ImGui.TextColored(plugin.GetThemeColor(), "📊 Popular Emotes 📊");
            ImGui.Separator();

            var sortedEmotes = plugin.Config.EmoteCounts.OrderByDescending(x => x.Value).ToList();

            if (sortedEmotes.Count > 0)
            {
                ImGui.Text($"Most received emote:");
                ImGui.SameLine();
                // Replaced hardcoded color for the top emote
                ImGui.TextColored(plugin.GetThemeColor(), $"{sortedEmotes[0].Key} ({sortedEmotes[0].Value} times)");

                if (sortedEmotes.Count > 1) ImGui.Text($"Second highest: {sortedEmotes[1].Key} ({sortedEmotes[1].Value})");
                if (sortedEmotes.Count > 2) ImGui.Text($"Third highest: {sortedEmotes[2].Key} ({sortedEmotes[2].Value})");
            }
            else
            {
                ImGui.TextDisabled("No emote data collected yet...");
            }
        }

        private void paparazzi()
        {
            ImGui.TextColored(plugin.GetThemeColor(), "✨ Paparazzi ✨");
            ImGui.Separator();

            var topAdmirer = plugin.Config.AdmirerCounts.OrderByDescending(x => x.Value).FirstOrDefault();
            var topChecker = plugin.Config.TargetCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            ImGui.Text("Most Emotes Received From:");
            ImGui.SameLine();
            // Replaced hardcoded name color
            ImGui.TextColored(plugin.GetThemeColor(), topAdmirer.Key ?? "None yet!");

            ImGui.Text("Most Checks From:");
            ImGui.SameLine();
            // Replaced hardcoded name color
            ImGui.TextColored(plugin.GetThemeColor(), topChecker.Key ?? "None yet!");
        }
    }
}
