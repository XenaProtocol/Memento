using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Memento.Windows.Tabs
{
    /// <summary>
    /// Help tab — collapsible reference sections explaining pet mechanics,
    /// emote stat effects, daily care actions, egg tiers, and observer system.
    /// </summary>
    public class HelpTab : IDrawablePage
    {
        private readonly Plugin plugin;
        public string TabLabel => " Help ";
        // Tracks which sections are collapsed — click header to toggle
        private readonly HashSet<string> collapsed = new();

        public HelpTab(Plugin plugin) { this.plugin = plugin; }

        public void Draw()
        {
            float pH = ImGui.GetContentRegionAvail().Y - 4f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##HelpOuter", new Vector2(0, pH), false))
            {
                float W = ImGui.GetContentRegionAvail().X; // inside child = excludes scrollbar
                var dl0 = ImGui.GetWindowDrawList();
                dl0.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
                ImGui.Spacing();

                // ── How Your Pet Works ────────────────────────────────────
                if (DrawSection("How Your Pet Works", W))
                {
                    ImGui.Spacing();
                    var dl = ImGui.GetWindowDrawList();
                    var pilTL = ImGui.GetCursorScreenPos();
                    var pilTLLocal = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(pilTLLocal + new Vector2(12f, 10f));

                    DrawKeyLine("Growth", MementoColors.Purple5, "Emotes received are the ONLY way to level up your pet.");
                    ImGui.Spacing();
                    DrawKeyLine("Stats", MementoColors.Pink, "Love · Joy · Energy · Spirit — each filled by different emotes.");
                    ImGui.Spacing();
                    DrawKeyLine("Daily Care", MementoColors.Green, "Fills the care meter. Adds up to +25% bonus to Overall Health.");
                    ImGui.Spacing();
                    DrawKeyLine("Decay", MementoColors.Amber, "Stats drop slowly. Above 50% care = zero decay.");
                    ImGui.Spacing();
                    DrawKeyLine("Overall Health", MementoColors.Teal, "Average of all 4 stats + care bonus. This is your pet's life bar.");
                    ImGui.Spacing();
                    DrawKeyLine("Favourite Emote", MementoColors.Pink, "Randomly chosen at hatch. Receiving it gives a massive 3.5x boost.");
                    ImGui.Spacing();
                    DrawKeyLine("Death", MementoColors.Red, "3 neglect days with Overall Health at zero = pet gone forever.");
                    ImGui.Spacing();
                    ImGui.Dummy(new Vector2(W, 8f));

                    var pilBR = ImGui.GetCursorScreenPos();
                    dl.AddRect(pilTL, new Vector2(pilTL.X + W, pilBR.Y),
                        MementoColors.ToU32(MementoColors.Border, 0.50f), 8f, ImDrawFlags.None, 0.8f);
                    dl.AddRectFilled(pilTL, new Vector2(pilTL.X + W, pilBR.Y),
                        MementoColors.ToU32(MementoColors.Panel2, 0.35f), 8f);
                }

                ImGui.Spacing(); ImGui.Spacing();

                // ── Emote Effects ─────────────────────────────────────────
                if (DrawSection("Emote Effects on Stats", W))
                {
                    ImGui.Spacing();
                    var dl = ImGui.GetWindowDrawList();
                    var hpos = ImGui.GetCursorScreenPos();
                    float c1 = hpos.X, c2 = hpos.X + 130f, c3 = hpos.X + 215f, c4 = hpos.X + 295f, c5 = hpos.X + 385f;
                    dl.AddText(new Vector2(c1, hpos.Y), MementoColors.ToU32(MementoColors.Dim), "Emote");
                    dl.AddText(new Vector2(c2, hpos.Y), MementoColors.ToU32(MementoColors.Pink), "Love");
                    dl.AddText(new Vector2(c3, hpos.Y), MementoColors.ToU32(MementoColors.Purple5), "Joy");
                    dl.AddText(new Vector2(c4, hpos.Y), MementoColors.ToU32(MementoColors.Teal), "Spirit");
                    dl.AddText(new Vector2(c5, hpos.Y), MementoColors.ToU32(MementoColors.Amber), "Energy");
                    ImGui.Dummy(new Vector2(W, ImGui.GetTextLineHeight() + 4f));

                    dl = ImGui.GetWindowDrawList();
                    float lineY = ImGui.GetCursorScreenPos().Y;
                    dl.AddLine(new Vector2(hpos.X, lineY), new Vector2(hpos.X + W - 4f, lineY),
                        MementoColors.ToU32(MementoColors.Border, 0.35f), 1f);
                    ImGui.Dummy(new Vector2(W, 4f));

                    DrawEmoteRow(W, "Dote", "Love +++", "Joy +", "Spirit +", "Energy +");
                    DrawEmoteRow(W, "Pet", "Love +++", "Joy ++", "Spirit +", "Energy +");
                    DrawEmoteRow(W, "Embrace", "Love ++", "Joy +++", "Spirit +++", "Energy +");
                    DrawEmoteRow(W, "Blow Kiss", "Love +++", "Joy +", "", "Energy +");
                    DrawEmoteRow(W, "Hug", "Love ++", "Joy ++", "Spirit +++", "");
                    DrawEmoteRow(W, "Flower Shower", "", "Joy +++", "Spirit +++", "Energy ++");
                    DrawEmoteRow(W, "Cheer", "", "Joy ++", "", "Energy +++");
                    DrawEmoteRow(W, "Comfort", "Love +", "", "Spirit +++", "");
                    DrawEmoteRow(W, "Poke", "", "Joy +", "", "Energy +");
                    DrawEmoteRow(W, "Furious", "Love ---", "Joy ---", "Spirit -", "", negative: true);
                    DrawEmoteRow(W, "Slap", "Love --", "Joy --", "", "Energy +", negative: true);

                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Dim with { W = 0.70f }, "Any other emote gives a small Love + Joy boost.");
                    ImGui.TextColored(MementoColors.Purple5, "All emotes affect your pet, even untracked ones.");
                    ImGui.TextColored(MementoColors.Teal, "Spirit fills from bonding emotes: Hug, Embrace, Comfort, Flower Shower.");
                    ImGui.TextColored(MementoColors.Dim with { W = 0.65f }, "Tracked emotes also appear in the Social tab log.");
                }

                ImGui.Spacing(); ImGui.Spacing();

                // ── Daily Care Actions ────────────────────────────────────
                if (DrawSection("Daily Care Actions", W))
                {
                    ImGui.Spacing();
                    DrawCareRow("♪", "Play with pet", "+15–20% care", "Once every 4 hours via the Play button", MementoColors.Purple5);
                    DrawCareRow("⚔", "Complete a duty", "+20% care", "Up to 3× per day — roulettes, raids, trials", MementoColors.Amber);
                    DrawCareRow("!", "Finish a quest", "+10% care", "Up to 5× per day — any main, side, or daily quest", MementoColors.Green);
                    DrawCareRow("⚑", "Join a party", "+5% care", "Once per day", MementoColors.Teal);
                    DrawCareRow("✿", "Enter GPose", "+10% care", "Up to 2× per day", MementoColors.Pink);
                    DrawCareRow("✦", "Craft something", "+5% care", "Up to 4× per day", MementoColors.Teal);
                    DrawCareRow("✕", "Kill mobs", "+1%/kill", "Capped at 20% per day", MementoColors.Red);
                    DrawCareRow("▲", "Level up", "Scales", "Lv 1–30: +5%  ·  Lv 30–60: +10%  ·  Lv 60–99: +20% (hidden at 100)", MementoColors.Purple6);
                    DrawCareRow("🌱", "Tend crops (Terra)", "+10% care", "First terra action per day — hidden if Terra not installed", MementoColors.Green);

                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Green, "Above 50% care:");
                    ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "no stat decay at all.");
                    ImGui.TextColored(MementoColors.Amber, "25–50% care:");
                    ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "slow decay.");
                    ImGui.TextColored(MementoColors.Red, "Below 25% care:");
                    ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "faster decay. Neglect streak increases.");
                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Teal, "Care bonus adds up to +25% to Overall Health on top of your stat average.");
                }

                ImGui.Spacing(); ImGui.Spacing();

                // ── Egg Tiers ─────────────────────────────────────────────
                if (DrawSection("Egg Tiers", W))
                {
                    ImGui.Spacing();
                    DrawEggRow("Gold", MementoColors.Gold, "Fox 50%  ·  Bunny 25%  ·  Sparrow 25%");
                    DrawEggRow("Silver", MementoColors.Dim, "Bunny 50%  ·  Fox 25%  ·  Sparrow 25%");
                    DrawEggRow("Bronze", MementoColors.Amber with { W = 0.7f }, "Sparrow 50%  ·  Fox 25%  ·  Bunny 25%");
                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Dim, "Egg tier is determined by your lifetime emotes received when hatching.");
                }

                ImGui.Spacing(); ImGui.Spacing();

                // ── Observer System ───────────────────────────────────────
                if (DrawSection("Who's Watching (Observer System)", W))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Dim, "The Social tab shows who is targeting you — live and historical.");
                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Purple5, "Chat notification"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "— echo message when someone targets you.");
                    ImGui.TextColored(MementoColors.Purple5, "Play sound"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "— audio ping on new target.");
                    ImGui.TextColored(MementoColors.Purple5, "Draw dot overhead"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "— renders a marker above targeting players in-world.");
                    ImGui.TextColored(MementoColors.Purple5, "Highlight nameplate"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Dim, "— colours their name in the world.");
                }

                ImGui.Spacing(); ImGui.Spacing();

                // ── Popout Window ─────────────────────────────────────────
                if (DrawSection("Popout Window", W))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Dim, "Type"); ImGui.SameLine();
                    ImGui.TextColored(MementoColors.Purple5, "/mementopet"); ImGui.SameLine();
                    ImGui.TextColored(MementoColors.Dim, "or click the paw icon in the title bar.");
                    ImGui.Spacing();
                    ImGui.TextColored(MementoColors.Dim, "The popout shows your pet + a live Recent Events feed.");
                    ImGui.TextColored(MementoColors.Dim, "Events include all emotes with stat deltas and care actions.");
                    ImGui.TextColored(MementoColors.Dim, "The popout follows your active colour theme.");
                }

                ImGui.Dummy(new Vector2(W, 16f));
                dl0.PopClipRect();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        /// <summary>
        /// Draws a collapsible section header. Returns true if the section is expanded.
        /// Click the header to toggle. Arrow indicator shows state.
        /// </summary>
        private bool DrawSection(string title, float W)
        {
            bool isCollapsed = collapsed.Contains(title);
            string arrow = isCollapsed ? "▸" : "▾";
            string display = $"{arrow}  {title.ToUpper()}";

            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float lh = ImGui.GetTextLineHeight();
            float headerH = lh + 12f;
            var hoverRect = new Vector2(W, headerH);

            // Place invisible button first so we can check hover state
            ImGui.SetCursorScreenPos(pos);
            if (ImGui.InvisibleButton($"##sect_{title}", hoverRect))
            {
                if (isCollapsed) collapsed.Remove(title);
                else collapsed.Add(title);
                isCollapsed = !isCollapsed;
            }
            bool hovered = ImGui.IsItemHovered();

            // Background — stronger when expanded, highlighted on hover
            float bgAlpha = isCollapsed ? 0.15f : 0.40f;
            if (hovered) bgAlpha += 0.15f;
            dl.AddRectFilled(pos, pos + hoverRect,
                MementoColors.ToU32(MementoColors.Panel2, bgAlpha), 6f);

            // Left accent bar when expanded
            if (!isCollapsed)
                dl.AddRectFilled(pos + new Vector2(0, 2f), pos + new Vector2(3f, headerH - 2f),
                    MementoColors.ToU32(MementoColors.Purple5, 0.80f), 2f);

            // Title text — purple when expanded, dim when collapsed
            var sz = ImGui.CalcTextSize(display);
            float tx = pos.X + (W - sz.X) / 2f;
            var textColor = isCollapsed
                ? (hovered ? MementoColors.Text : MementoColors.Dim)
                : MementoColors.Purple5;
            dl.AddText(new Vector2(tx, pos.Y + 6f), MementoColors.ToU32(textColor), display);

            // Side lines
            float ly = pos.Y + headerH / 2f;
            uint lc = MementoColors.ToU32(MementoColors.Dim, 0.18f);
            dl.AddLine(new Vector2(pos.X + 6f, ly), new Vector2(tx - 10f, ly), lc, 1f);
            dl.AddLine(new Vector2(tx + sz.X + 10f, ly), new Vector2(pos.X + W - 6f, ly), lc, 1f);

            ImGui.SetCursorScreenPos(pos + new Vector2(0, headerH));
            return !isCollapsed;
        }

        private void DrawKeyLine(string key, Vector4 color, string description)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 12f);
            ImGui.TextColored(color, key); ImGui.SameLine(0, 6f);
            ImGui.TextColored(MementoColors.Dim, $"— {description}");
        }

        private void DrawEmoteRow(float W, string emote, string love, string joy,
            string spirit, string energy, bool negative = false)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float h = 24f;

            if ((int)(ImGui.GetCursorPos().Y / h) % 2 == 0)
                dl.AddRectFilled(pos, pos + new Vector2(W - 4f, h),
                    MementoColors.ToU32(MementoColors.Text, 0.025f), 3f);

            float baseY = pos.Y + (h - ImGui.GetTextLineHeight()) / 2f;
            var dotColor = negative ? MementoColors.Red : MementoColors.GetEmoteColor(emote);
            dl.AddCircleFilled(pos + new Vector2(8f, h / 2f), 3.5f, MementoColors.ToU32(dotColor));
            dl.AddText(new Vector2(pos.X + 18f, baseY), MementoColors.ToU32(MementoColors.Text), emote);
            if (!string.IsNullOrEmpty(love)) dl.AddText(new Vector2(pos.X + 130f, baseY), MementoColors.ToU32(MementoColors.Pink), love);
            if (!string.IsNullOrEmpty(joy)) dl.AddText(new Vector2(pos.X + 215f, baseY), MementoColors.ToU32(MementoColors.Purple5), joy);
            if (!string.IsNullOrEmpty(spirit)) dl.AddText(new Vector2(pos.X + 295f, baseY), MementoColors.ToU32(MementoColors.Teal), spirit);
            if (!string.IsNullOrEmpty(energy)) dl.AddText(new Vector2(pos.X + 385f, baseY), MementoColors.ToU32(MementoColors.Amber), energy);
            ImGui.Dummy(new Vector2(W, h));
        }

        private void DrawCareRow(string icon, string action, string amount,
            string detail, Vector4 color)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float W = ImGui.GetContentRegionAvail().X, h = 42f;
            dl.AddRectFilled(pos, pos + new Vector2(W, h),
                MementoColors.ToU32(MementoColors.Panel, 0.40f), 6f);
            dl.AddText(new Vector2(pos.X + 10f, pos.Y + 11f), MementoColors.ToU32(color), icon);
            dl.AddText(new Vector2(pos.X + 30f, pos.Y + 11f), MementoColors.ToU32(MementoColors.Text), action);
            dl.AddText(new Vector2(pos.X + 30f, pos.Y + 25f), MementoColors.ToU32(MementoColors.Dim), detail);
            var aSz = ImGui.CalcTextSize(amount);
            dl.AddText(new Vector2(pos.X + W - aSz.X - 10f, pos.Y + (h - aSz.Y) / 2f),
                MementoColors.ToU32(color), amount);
            ImGui.Dummy(new Vector2(W, h));
            ImGui.Spacing();
        }

        private void DrawEggRow(string tier, Vector4 color, string detail)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 4f);
            ImGui.TextColored(color, $"🥚 {tier}"); ImGui.SameLine(0, 8f);
            ImGui.TextColored(MementoColors.Dim, $"— {detail}");
        }
    }
}
