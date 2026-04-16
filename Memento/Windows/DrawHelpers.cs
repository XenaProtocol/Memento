using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Memento.Windows
{
    /// <summary>
    /// Memento-specific draw helpers that depend on MementoColors semantic
    /// colours (emote tags, log rows, admirer cards).
    ///
    /// Generic helpers (SectionTitle, MetricCard, StatBar, etc.) live in
    /// EnzirionCore.UI.DrawHelpers and take a ThemePalette parameter.
    /// Call those via EnzirionCore.UI.DrawHelpers.Method(MementoColors.Palette, ...).
    /// </summary>
    public static class DrawHelpers
    {
        // -----------------------------------------------
        // Log row for the Social tab — parses "[time] Player used Emote on you!"
        // format into: colored dot · timestamp · first name · "used" · emote pill tag · icon
        // -----------------------------------------------
        public static void LogRow(string entry, bool alt)
        {
            string timeStr = "", who = "", emote = "";
            try
            {
                int lb = entry.IndexOf('['), rb = entry.IndexOf(']');
                if (lb >= 0 && rb > lb)
                {
                    timeStr = entry.Substring(lb + 1, rb - lb - 1);
                    string rest = entry.Substring(rb + 2).Trim();
                    int ui = rest.IndexOf(" used ");
                    if (ui > 0)
                    {
                        who = rest.Substring(0, ui);
                        string after = rest.Substring(ui + 6);
                        int oi = after.IndexOf(" on you");
                        emote = oi > 0 ? after.Substring(0, oi) : after.TrimEnd('!');
                    }
                }
            }
            catch { emote = entry; }

            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetContentRegionAvail().X;
            float h = 26f;

            var rowBg = alt
                ? MementoColors.Panel2 with { W = 0.85f }
                : MementoColors.Panel with { W = 0.70f };
            dl.AddRectFilled(pos, pos + new Vector2(w, h),
                MementoColors.ToU32(rowBg), 5f);

            var dotColor = MementoColors.GetEmoteColor(emote);
            dl.AddCircleFilled(pos + new Vector2(10f, h / 2f), 4f,
                MementoColors.ToU32(dotColor));

            float baseY = pos.Y + (h - ImGui.GetTextLineHeight()) / 2f;

            dl.AddText(new Vector2(pos.X + 22f, baseY),
                MementoColors.ToU32(MementoColors.Dim), timeStr);

            float whoX = pos.X + 80f;
            string firstName = who.Contains(' ') ? who.Split(' ')[0] : who;
            dl.AddText(new Vector2(whoX, baseY),
                MementoColors.ToU32(MementoColors.Purple5), firstName);

            float usedX = whoX + ImGui.CalcTextSize(firstName).X + 6f;
            dl.AddText(new Vector2(usedX, baseY),
                MementoColors.ToU32(MementoColors.Dim), "used");

            float tagX = usedX + ImGui.CalcTextSize("used ").X;
            EmoteTag(dl, new Vector2(tagX, pos.Y + 4f), emote);

            string icon = MementoColors.GetEmoteIcon(emote);
            dl.AddText(new Vector2(pos.X + w - 18f, baseY),
                MementoColors.ToU32(dotColor), icon);

            ImGui.Dummy(new Vector2(w, h));
        }

        // -----------------------------------------------
        // Emote pill tag
        // -----------------------------------------------
        public static void EmoteTag(ImDrawListPtr dl, Vector2 pos, string emote)
        {
            var (bg, fg) = MementoColors.GetEmoteTagColors(emote);
            var sz = ImGui.CalcTextSize(emote);
            float ph = 6f, pv = 3f;
            var br = pos + new Vector2(sz.X + ph * 2, sz.Y + pv * 2);
            dl.AddRectFilled(pos, br, MementoColors.ToU32(bg), 8f);
            dl.AddRect(pos, br, MementoColors.ToU32(fg, 0.35f), 8f, ImDrawFlags.None, 0.8f);
            dl.AddText(pos + new Vector2(ph, pv), MementoColors.ToU32(fg), emote);
        }

        // -----------------------------------------------
        // Admirer card — shows a player who interacted with you.
        // Rank 1 gets a gold border + "#1 Fan" badge. Avatar is a
        // colored circle with the player's initials.
        // -----------------------------------------------
        private static readonly Vector4[] AvatarColors =
        {
            new(0.36f, 0.14f, 0.66f, 1f),
            new(0.80f, 0.20f, 0.44f, 1f),
            new(0.10f, 0.50f, 0.70f, 1f),
            new(0.20f, 0.60f, 0.40f, 1f),
        };

        public static void AdmirerCard(string name, int count, int rank)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float w = ImGui.GetContentRegionAvail().X;
            float h = 52f;

            var bgColor = rank == 1
                ? new Vector4(0.22f, 0.10f, 0.06f, 0.95f)
                : MementoColors.Panel2 with { W = 0.95f };
            var bdColor = rank == 1 ? MementoColors.Amber : MementoColors.Border;

            dl.AddRectFilled(pos, pos + new Vector2(w, h),
                MementoColors.ToU32(bgColor), 8f);
            dl.AddRect(pos, pos + new Vector2(w, h),
                MementoColors.ToU32(bdColor), 8f, ImDrawFlags.None, rank == 1 ? 1.5f : 1f);

            var avColor = AvatarColors[(rank - 1) % AvatarColors.Length];
            var avCtr = pos + new Vector2(30f, h / 2f);
            dl.AddCircleFilled(avCtr, 16f, MementoColors.ToU32(avColor));

            string initials = name.Contains(' ')
                ? $"{name[0]}{name.Split(' ')[1][0]}"
                : name[..Math.Min(2, name.Length)].ToUpper();
            var initSz = ImGui.CalcTextSize(initials);
            dl.AddText(avCtr - initSz / 2f,
                MementoColors.ToU32(MementoColors.Text), initials);

            float tx = pos.X + 54f;
            dl.AddText(new Vector2(tx, pos.Y + 10f), MementoColors.ToU32(MementoColors.Text), name);
            dl.AddText(new Vector2(tx, pos.Y + 28f), MementoColors.ToU32(MementoColors.Dim), $"{count} interactions");

            if (rank == 1)
            {
                string badge = "#1 Fan";
                var badgeSz = ImGui.CalcTextSize(badge);
                float bx = pos.X + w - badgeSz.X - 22f;
                float by = pos.Y + (h - badgeSz.Y - 6f) / 2f;
                var bTL = new Vector2(bx - 6f, by);
                var bBR = new Vector2(bx + badgeSz.X + 6f, by + badgeSz.Y + 6f);
                dl.AddRectFilled(bTL, bBR,
                    MementoColors.ToU32(MementoColors.Pink, 0.18f), 10f);
                dl.AddRect(bTL, bBR,
                    MementoColors.ToU32(MementoColors.Pink, 0.40f), 10f, ImDrawFlags.None, 0.8f);
                dl.AddText(new Vector2(bx, by + 3f),
                    MementoColors.ToU32(MementoColors.Pink), badge);
            }

            ImGui.Dummy(new Vector2(w, h));
        }
    }
}
