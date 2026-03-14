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

            // Calculate total interactions
            int totalInteractions = plugin.Config.EmoteCounts.Values.Sum();
            string totalText = $"Total Admirations: {totalInteractions}";

            // Align to the right
            float posX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(totalText).X;
            ImGui.SameLine(posX);

            ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.5f, 1.0f), totalText);
        }

        private void trackedEmotes()
        {
            // --- TRACKED EMOTES SECTION ---
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.75f, 1.0f), "🎯 Tracked Emote Totals 🎯");
            ImGui.Separator();

            if (ImGui.BeginChild("TrackedTotals", new Vector2(0, -35), true))
            {
                // The magic RowBg flag strikes again!
                if (ImGui.BeginTable("StatsTable", 2, ImGuiTableFlags.RowBg))
                {
                    // Column 1 stretches for the name, Column 2 stays fixed for the number
                    ImGui.TableSetupColumn("EmoteName", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 40f);

                    foreach (var tracked in plugin.Config.TrackedEmotes)
                    {
                        int count = plugin.Config.EmoteCounts.GetValueOrDefault(tracked);

                        ImGui.TableNextRow();

                        // Column 1: The Emote Name
                        ImGui.TableNextColumn();
                        ImGui.Text($"{tracked}:");

                        // Column 2: The Total Count
                        ImGui.TableNextColumn();
                        ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), count.ToString());
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
        }

        private void popularEmotes()
        {
            // --- POPULAR EMOTES SECTION ---
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.75f, 1.0f), "📊 Popular Emotes 📊");
            ImGui.Separator();

            var sortedEmotes = plugin.Config.EmoteCounts.OrderByDescending(x => x.Value).ToList();

            if (sortedEmotes.Count > 0)
            {
                ImGui.Text($"Most received emote:");
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), $"{sortedEmotes[0].Key} ({sortedEmotes[0].Value} times)");

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
            // --- PAPARAZZI SECTION ---
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.75f, 1.0f), "✨ Paparazzi ✨");
            ImGui.Separator();

            var topAdmirer = plugin.Config.AdmirerCounts.OrderByDescending(x => x.Value).FirstOrDefault();
            var topChecker = plugin.Config.TargetCounts.OrderByDescending(x => x.Value).FirstOrDefault();

            ImGui.Text("Most Emotes Received From:");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), topAdmirer.Key ?? "None yet!");

            ImGui.Text("Most Checks From:");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.6f, 1.0f), topChecker.Key ?? "None yet!");
        }
    }
}
