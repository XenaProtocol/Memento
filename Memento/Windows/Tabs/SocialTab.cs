using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Memento.Windows.Tabs
{
    /// <summary>
    /// Social tab — emote log, best friends leaderboard, observer list
    /// (who's targeting you), and summary metric cards.
    /// </summary>
    public class SocialTab : IDrawablePage
    {
        private readonly Plugin plugin;
        public string TabLabel => " Social ";
        private int filterMode = 0;          // 0 = All, 1 = Emotes only, 2 = Observer only
        private string? pendingClear = null;  // Triggers confirmation popup for data wipe

        public SocialTab(Plugin plugin) { this.plugin = plugin; }

        public void Draw()
        {
            float pH = ImGui.GetContentRegionAvail().Y - 4f;
            var clipMin = ImGui.GetCursorScreenPos();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##SocialScroll", new Vector2(0, pH), false))
            {
                float W = ImGui.GetContentRegionAvail().X;
                var dl0 = ImGui.GetWindowDrawList();
                dl0.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
                ImGui.Spacing();
                DrawSummaryCards(W);
                ImGui.Spacing(); ImGui.Spacing();
                DrawBestFriends(W);
                ImGui.Spacing(); ImGui.Spacing();
                DrawEmoteBreakdown(W);
                ImGui.Spacing(); ImGui.Spacing();
                DrawObserverSection(W);
                ImGui.Dummy(new Vector2(W, 20f));
                dl0.PopClipRect();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        // ── Summary cards ─────────────────────────────────────────────────
        private void DrawSummaryCards(float W)
        {
            // Explicit widths to prevent overflow
            float gap = 6f;
            float cardW = (W - gap * 2f) / 3f;
            float cardH = 72f;
            int total = plugin.CharData.EmoteCounts.Values.Sum();
            int unique = plugin.CharData.AdmirerCounts.Count;
            int observers = plugin.CharData.TargetCounts.Count;

            EnzirionCore.UI.DrawHelpers.MetricCard(MementoColors.Palette, "Total Adorations", total.ToString(), "All time", cardW, cardH, MementoColors.Pink);
            ImGui.SameLine(0, gap);
            EnzirionCore.UI.DrawHelpers.MetricCard(MementoColors.Palette, "Unique Admirers", unique.ToString(), "Tracked players", cardW, cardH, MementoColors.JoyPurple);
            ImGui.SameLine(0, gap);
            EnzirionCore.UI.DrawHelpers.MetricCard(MementoColors.Palette, "Checked Out By", observers.ToString(), "Unique players", cardW, cardH, MementoColors.Teal);
        }

        // ── Best Friends (was Top Admirers) ──────────────────────────────
        private void DrawBestFriends(float W)
        {
            EnzirionCore.UI.DrawHelpers.SectionTitle(MementoColors.Palette, "Best Friends", W);
            ImGui.Spacing();

            int rank = 0;
            foreach (var kv in plugin.CharData.AdmirerCounts.OrderByDescending(x => x.Value).Take(5))
            {
                DrawFriendCard(kv.Key, kv.Value, ++rank, W);
                ImGui.Spacing();
            }
            if (!plugin.CharData.AdmirerCounts.Any())
            {
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize("No interactions yet...").X) / 2f);
                ImGui.TextColored(MementoColors.Dim, "No interactions yet...");
            }

            ImGui.Spacing();
            float btnW = 110f;
            ImGui.SetCursorPosX(W - btnW);
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Red with { W = 0.15f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Red with { W = 0.30f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Red);
            if (ImGui.Button("Clear Log##bf", new Vector2(btnW, 24f)))
                pendingClear = "Social";
            ImGui.PopStyleColor(3);
        }

        private void DrawFriendCard(string name, int count, int rank, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float h = 52f;

            // Light-mode safe: use a subtle gold tint instead of dark brown
            bool isDay = plugin.Config.Theme.EndsWith("(Day)");
            var bgColor = rank == 1
                ? (isDay
                    ? new Vector4(1.00f, 0.95f, 0.82f, 0.95f)   // warm cream for day
                    : new Vector4(0.22f, 0.10f, 0.06f, 0.95f))  // dark amber for night
                : MementoColors.Panel2 with { W = 0.95f };
            var bdColor = rank == 1 ? MementoColors.Gold : MementoColors.Border;

            dl.AddRectFilled(pos, pos + new Vector2(W, h), MementoColors.ToU32(bgColor), 8f);
            dl.AddRect(pos, pos + new Vector2(W, h), MementoColors.ToU32(bdColor), 8f, ImDrawFlags.None, rank == 1 ? 1.5f : 1f);

            uint[] avColors = {
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.36f,0.14f,0.66f,1f)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.80f,0.20f,0.44f,1f)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f,0.50f,0.70f,1f)),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f,0.60f,0.40f,1f)),
            };
            var avCtr = pos + new Vector2(28f, h / 2f);
            dl.AddCircleFilled(avCtr, 16f, avColors[(rank - 1) % avColors.Length]);
            string initials = name.Contains(' ')
                ? $"{name[0]}{name.Split(' ')[1][0]}"
                : name[..Math.Min(2, name.Length)].ToUpper();
            var initSz = ImGui.CalcTextSize(initials);
            dl.AddText(avCtr - initSz / 2f, MementoColors.ToU32(MementoColors.Text), initials);

            // Truncate name to avoid overflow
            string displayName = name.Length > 24 ? name[..24] : name;
            dl.AddText(new Vector2(pos.X + 52f, pos.Y + 10f),
                MementoColors.ToU32(rank == 1 && isDay ? new Vector4(0.50f, 0.32f, 0.02f, 1f) : MementoColors.Text),
                displayName);

            string mostRecent = "";
            foreach (var e in plugin.CharData.SocialLog)
            {
                if (e.Contains(name.Split(' ')[0]))
                {
                    try
                    {
                        int ui = e.IndexOf(" used "); int oi = e.IndexOf(" on you");
                        if (ui > 0 && oi > ui) mostRecent = e.Substring(ui + 6, oi - ui - 6);
                    }
                    catch { }
                    break;
                }
            }
            string subText = $"{count} emotes" + (string.IsNullOrEmpty(mostRecent) ? "" : $"  ·  last: {mostRecent}");
            // Clamp sub text width
            float maxSubW = W - 52f - 80f;
            while (subText.Length > 4 && ImGui.CalcTextSize(subText).X > maxSubW)
                subText = subText[..^4] + "…";
            dl.AddText(new Vector2(pos.X + 52f, pos.Y + 28f),
                MementoColors.ToU32(MementoColors.Dim), subText);

            if (rank == 1)
            {
                string badge = "#1 Friend";
                var bSz = ImGui.CalcTextSize(badge);
                float bx = pos.X + W - bSz.X - 22f, by = pos.Y + (h - bSz.Y - 6f) / 2f;
                var bTL = new Vector2(bx - 6f, by); var bBR = new Vector2(bx + bSz.X + 6f, by + bSz.Y + 6f);
                dl.AddRectFilled(bTL, bBR, MementoColors.ToU32(MementoColors.Pink, 0.18f), 10f);
                dl.AddRect(bTL, bBR, MementoColors.ToU32(MementoColors.Pink, 0.40f), 10f, ImDrawFlags.None, 0.8f);
                dl.AddText(new Vector2(bx, by + 3f), MementoColors.ToU32(MementoColors.Pink), badge);
            }

            ImGui.Dummy(new Vector2(W, h));
        }

        // ── Emote breakdown ───────────────────────────────────────────────
        private void DrawEmoteBreakdown(float W)
        {
            EnzirionCore.UI.DrawHelpers.SectionTitle(MementoColors.Palette, "Emote Breakdown", W);
            ImGui.Spacing();

            int maxC = plugin.CharData.EmoteCounts.Values.DefaultIfEmpty(1).Max();
            int rank = 0;
            foreach (var kv in plugin.CharData.EmoteCounts.OrderByDescending(x => x.Value).Take(6))
            {
                var color = ++rank switch
                { 1 => MementoColors.Pink, 2 => MementoColors.Purple5, 3 => MementoColors.Teal, _ => MementoColors.Dim };
                DrawEmoteBar(kv.Key, kv.Value, maxC, rank, color, W);
            }
            if (!plugin.CharData.EmoteCounts.Any())
            {
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize("No emotes recorded yet...").X) / 2f);
                ImGui.TextColored(MementoColors.Dim, "No emotes recorded yet...");
            }
        }

        private void DrawEmoteBar(string name, int count, int max, int rank, Vector4 color, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float h = 22f, rkW = 18f, lblW = 120f, numW = 36f;
            float barW = W - rkW - lblW - numW - 16f;

            dl.AddText(new Vector2(pos.X, pos.Y + 3f),
                MementoColors.ToU32(MementoColors.Dim), $"{rank}");
            dl.AddText(new Vector2(pos.X + rkW, pos.Y + 3f),
                MementoColors.ToU32(MementoColors.Text), name);

            var barTL = new Vector2(pos.X + rkW + lblW, pos.Y + 7f);
            var barBR = new Vector2(barTL.X + barW, barTL.Y + 5f);
            dl.AddRectFilled(barTL, barBR, MementoColors.ToU32(MementoColors.Text, 0.06f), 3f);
            float fill = max > 0 ? (float)count / max : 0f;
            if (fill > 0f)
                dl.AddRectFilled(barTL, new Vector2(barTL.X + barW * fill, barBR.Y),
                    MementoColors.ToU32(color), 3f);
            dl.AddText(new Vector2(barBR.X + 5f, pos.Y + 3f),
                MementoColors.ToU32(color), count.ToString());
            ImGui.Dummy(new Vector2(W, h));
        }

        // ── Observer section ──────────────────────────────────────────────
        private void DrawObserverSection(float W)
        {
            var obs = plugin.CharData.ObserverSettings;
            int nowCount = plugin.CharData.CurrentlyTargeting.Count;

            // Section header
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            dl.AddText(pos, MementoColors.ToU32(MementoColors.Dim), "WHO'S WATCHING");
            float lh = ImGui.GetTextLineHeight();
            float titleW = ImGui.CalcTextSize("WHO'S WATCHING").X;

            // Badge flush right
            string badge = $"{nowCount} now targeting";
            var bSz = ImGui.CalcTextSize(badge);
            float bx = pos.X + W - bSz.X - 16f, by = pos.Y;
            dl.AddRectFilled(new Vector2(bx - 8f, by), new Vector2(bx + bSz.X + 8f, by + lh + 4f),
                MementoColors.ToU32(MementoColors.Panel2), 10f);
            dl.AddRect(new Vector2(bx - 8f, by), new Vector2(bx + bSz.X + 8f, by + lh + 4f),
                MementoColors.ToU32(MementoColors.Teal, 0.40f), 10f, ImDrawFlags.None, 0.8f);
            dl.AddText(new Vector2(bx, by + 2f), MementoColors.ToU32(MementoColors.Teal), badge);

            uint lc = MementoColors.ToU32(MementoColors.Dim, 0.18f);
            dl.AddLine(new Vector2(pos.X + titleW + 10f, pos.Y + lh / 2f),
                new Vector2(bx - 16f, pos.Y + lh / 2f), lc, 1f);
            ImGui.Dummy(new Vector2(W, lh + 8f));

            // Observer toggles — strict 2×2 grid
            ImGui.Spacing();
            float togW2 = MathF.Floor((W - 8f) / 2f); // exact half, no rounding drift

            var startPos = ImGui.GetCursorScreenPos();
            var startLocalPos = ImGui.GetCursorPos(); // window-local, scroll-safe
            bool chat = obs.ChatNotification, sound = obs.PlaySound,
                 dot = obs.DrawDotOverhead, plate = obs.HighlightNameplate;

            // Row 1
            ImGui.SetCursorPos(startLocalPos);
            DrawObsToggle("Chat notification", "Echo when targeted", ref chat, togW2);
            ImGui.SetCursorPos(new Vector2(startLocalPos.X + togW2 + 8f, startLocalPos.Y));
            DrawObsToggle("Play sound", "Ping on new target", ref sound, togW2);

            // Row 2
            ImGui.SetCursorPos(new Vector2(startLocalPos.X, startLocalPos.Y + 44f + 4f));
            DrawObsToggle("Draw dot overhead", "Marker above targeting players", ref dot, togW2);
            ImGui.SetCursorPos(new Vector2(startLocalPos.X + togW2 + 8f, startLocalPos.Y + 44f + 4f));
            DrawObsToggle("Highlight nameplate", "Colour their name in world", ref plate, togW2);

            // Advance cursor past both rows
            ImGui.SetCursorPos(new Vector2(startLocalPos.X, startLocalPos.Y + 44f * 2f + 4f + 4f));

            if (chat != obs.ChatNotification || sound != obs.PlaySound ||
                dot != obs.DrawDotOverhead || plate != obs.HighlightNameplate)
            {
                obs.ChatNotification = chat; obs.PlaySound = sound;
                obs.DrawDotOverhead = dot; obs.HighlightNameplate = plate;
                plugin.SaveCharData();
            }

            ImGui.Spacing(); ImGui.Spacing();

            // Filter pills
            string[] filterLabels = { "All", "Targeting me now", "Emoted on me", "Frequent visitors" };
            DrawFilterPills(filterLabels, ref filterMode, W);
            ImGui.Spacing();

            // Observer list — fixed height scrollable, account for scrollbar
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##obsList", new Vector2(W, 220f), false, ImGuiWindowFlags.NoNav))
            {
                float listW = ImGui.GetContentRegionAvail().X;
                DrawObserverList(listW);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.Spacing();
            float clearW = 150f;
            ImGui.SetCursorPosX(W - clearW);
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Red with { W = 0.15f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Red with { W = 0.30f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Red);
            if (ImGui.Button("Clear Observer Log", new Vector2(clearW, 24f)))
                pendingClear = "Observer";

            DrawClearConfirmPopup();
            ImGui.PopStyleColor(3);
        }

        private void DrawObsToggle(string label, string sub, ref bool value, float w)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var localPos = ImGui.GetCursorPos();
            float h = 44f, togW = 32f, togH = 18f;

            dl.AddRectFilled(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Panel2), 8f);
            dl.AddRect(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Border), 8f, ImDrawFlags.None, 0.8f);

            // Truncate label to fit
            string lbl = label;
            float maxLblW = w - togW - 24f;
            while (lbl.Length > 4 && ImGui.CalcTextSize(lbl).X > maxLblW) lbl = lbl[..^1];

            dl.AddText(new Vector2(pos.X + 10f, pos.Y + 8f), MementoColors.ToU32(MementoColors.Text), lbl);
            dl.AddText(new Vector2(pos.X + 10f, pos.Y + 24f), MementoColors.ToU32(MementoColors.Dim with { W = 0.65f }), sub.Length > 22 ? sub[..22] + "…" : sub);

            var togPos = new Vector2(pos.X + w - togW - 8f, pos.Y + (h - togH) / 2f);
            dl.AddRectFilled(togPos, togPos + new Vector2(togW, togH),
                MementoColors.ToU32(value ? MementoColors.Purple5 : new Vector4(0.30f, 0.30f, 0.40f, 0.45f)), 10f);
            float thumbX = value ? togPos.X + togW - togH + 2f : togPos.X + 2f;
            dl.AddCircleFilled(new Vector2(thumbX + togH / 2f - 2f, togPos.Y + togH / 2f),
                togH / 2f - 2f, MementoColors.ToU32(new Vector4(1f, 1f, 1f, 0.95f)));

            ImGui.Dummy(new Vector2(w, h));
            // Place invisible button over the toggle using screen pos (safe — same frame, not scrolled mid-draw)
            ImGui.SetCursorScreenPos(togPos);
            if (ImGui.InvisibleButton($"##obstog_{label}", new Vector2(togW, togH)))
            { value = !value; plugin.SaveCharData(); }
            // Restore cursor using local coords
            ImGui.SetCursorPos(new Vector2(localPos.X, localPos.Y + h));
        }

        private void DrawFilterPills(string[] labels, ref int selected, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var startLocal = ImGui.GetCursorPos();
            float x = ImGui.GetCursorScreenPos().X;
            float y = ImGui.GetCursorScreenPos().Y;
            float ph = ImGui.GetTextLineHeight() + 8f;

            for (int i = 0; i < labels.Length; i++)
            {
                var sz = ImGui.CalcTextSize(labels[i]);
                float pw = sz.X + 16f;
                bool active = selected == i;
                var tl = new Vector2(x, y); var br = new Vector2(x + pw, y + ph);
                dl.AddRectFilled(tl, br, MementoColors.ToU32(active ? MementoColors.Purple5 with { W = 0.90f } : MementoColors.Panel2), 14f);
                dl.AddRect(tl, br, MementoColors.ToU32(active ? MementoColors.Purple5 : MementoColors.Border), 14f, ImDrawFlags.None, 0.8f);
                dl.AddText(tl + new Vector2(8f, 4f),
                    MementoColors.ToU32(active ? new Vector4(0.1f, 0.05f, 0.2f, 1f) : MementoColors.Dim), labels[i]);
                ImGui.SetCursorScreenPos(tl);
                if (ImGui.InvisibleButton($"##fp{i}", new Vector2(pw, ph))) selected = i;
                x = br.X + 6f;
            }
            // Advance cursor past pills using local coords
            ImGui.SetCursorPos(new Vector2(startLocal.X, startLocal.Y + ph + 4f));
        }

        private void DrawObserverList(float W)
        {
            var allObservers = plugin.CharData.TargetCounts
                .OrderByDescending(x => x.Value).ToList();

            var filtered = filterMode switch
            {
                1 => allObservers.Where(x => plugin.CharData.CurrentlyTargeting.Contains(x.Key)).ToList(),
                2 => allObservers.Where(x => plugin.CharData.AdmirerCounts.ContainsKey(x.Key)).ToList(),
                3 => allObservers.Where(x => x.Value >= 3).ToList(),
                _ => allObservers,
            };

            if (!filtered.Any())
            {
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize("No observers match this filter...").X) / 2f);
                ImGui.TextColored(MementoColors.Dim, "No observers match this filter...");
                return;
            }

            var dl = ImGui.GetWindowDrawList();
            var clipMin = ImGui.GetWindowPos();
            var clipMax = clipMin + ImGui.GetWindowSize();
            dl.PushClipRect(clipMin, clipMax, true);
            bool alt = false;
            foreach (var kv in filtered.Take(20))
            {
                bool isLive = plugin.CharData.CurrentlyTargeting.Contains(kv.Key);
                plugin.CharData.TargetToday.TryGetValue(kv.Key, out int todayCount);

                var pos = ImGui.GetCursorScreenPos();
                float h = 26f;

                dl.AddRectFilled(pos, pos + new Vector2(W, h),
                    MementoColors.ToU32(alt ? MementoColors.Panel2 : MementoColors.Panel, 0.80f), 4f);

                uint dotColor = isLive
                    ? MementoColors.ToU32(MementoColors.Teal)
                    : MementoColors.ToU32(MementoColors.Dim, 0.40f);
                dl.AddCircleFilled(pos + new Vector2(10f, h / 2f), 4f, dotColor);

                float baseY = pos.Y + (h - ImGui.GetTextLineHeight()) / 2f;
                string fn = kv.Key.Contains(' ')
                    ? kv.Key.Split(' ')[0] + " " + kv.Key.Split(' ')[1][0] + "."
                    : kv.Key;

                string todayStr = todayCount > 0 ? $"{todayCount}x today" : "not today";
                string totalStr = $"{kv.Value} total";
                var tSz = ImGui.CalcTextSize(totalStr);
                float todayW = ImGui.CalcTextSize(todayStr).X;
                float maxNameW = W - 22f - todayW - tSz.X - 40f;
                while (fn.Length > 2 && ImGui.CalcTextSize(fn).X > maxNameW) fn = fn[..^1];
                if (ImGui.CalcTextSize(fn).X > maxNameW && fn.Length > 2) fn = fn[..^1] + "\u2026";

                dl.AddText(new Vector2(pos.X + 22f, baseY),
                    MementoColors.ToU32(MementoColors.Purple5), fn);
                dl.AddText(new Vector2(pos.X + 22f + ImGui.CalcTextSize(fn).X + 10f, baseY),
                    MementoColors.ToU32(MementoColors.Dim), todayStr);
                dl.AddText(new Vector2(pos.X + W - tSz.X - 8f, baseY),
                    MementoColors.ToU32(isLive ? MementoColors.Teal : MementoColors.Dim), totalStr);

                ImGui.Dummy(new Vector2(W, h));
                alt = !alt;
            }
            dl.PopClipRect();
        }

        private void DrawClearConfirmPopup()
        {
            if (pendingClear == null) return;
            ImGui.OpenPopup("##socialConfirm");
            ImGui.SetNextWindowSize(new Vector2(300f, 130f), ImGuiCond.Always);
            bool open = true;
            if (ImGui.BeginPopupModal("##socialConfirm", ref open,
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                string what = pendingClear == "Social"
                    ? "clear the Best Friends log?"
                    : "clear all observer history?";
                ImGui.Spacing();
                ImGui.SetCursorPosX(12f);
                ImGui.TextColored(MementoColors.Red, "Are you sure you want to");
                ImGui.SetCursorPosX(12f);
                ImGui.TextColored(MementoColors.Text, what);
                ImGui.Spacing();
                ImGui.SetCursorPosX(12f);
                ImGui.TextColored(MementoColors.Dim with { W = 0.65f }, "This cannot be undone.");
                ImGui.Spacing(); ImGui.Spacing();
                ImGui.SetCursorPosX(12f);

                ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Red with { W = 0.20f });
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Red with { W = 0.38f });
                ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Red);
                if (ImGui.Button("Yes, clear##scyes", new Vector2(120f, 26f)))
                {
                    if (pendingClear == "Social")
                    { plugin.CharData.SocialLog.Clear(); plugin.CharData.EmoteCounts.Clear(); plugin.CharData.AdmirerCounts.Clear(); plugin.SaveCharData(); }
                    else
                    { plugin.CharData.TargetCounts.Clear(); plugin.CharData.TargetToday.Clear(); plugin.SaveCharData(); }
                    pendingClear = null;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine(0, 8f);
                if (ImGui.Button("Cancel##sccancel", new Vector2(90f, 26f)))
                { pendingClear = null; ImGui.CloseCurrentPopup(); }
                if (!open) pendingClear = null;
                ImGui.EndPopup();
            }
        }
    }
}
