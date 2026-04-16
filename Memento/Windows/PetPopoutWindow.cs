using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Memento.Pet;

namespace Memento.Windows
{
    /// <summary>
    /// Compact floating window showing the pet sprite + stats on the left
    /// and a live recent-events feed on the right. Opened via /mementopet.
    /// Follows the active colour theme. Entirely draw-list rendered (no ImGui widgets).
    /// </summary>
    public class PetPopoutWindow : StandardWindow
    {
        protected override ThemePalette GetCurrentPalette() =>
            ThemeRegistry.GetByLegacyName(plugin.Config.Theme) ?? ThemeRegistry.PerfectlyPurple;

        private readonly Plugin plugin;

        // Layout constants — left panel (pet) + right panel (events) + padding
        private const float PetW = 200f;
        private const float FeedW = 230f;
        private const float WinH = 270f;
        private const float Pad = 10f;

        private static readonly uint CloseNormal = ImGui.ColorConvertFloat4ToU32(new Vector4(0.75f, 0.18f, 0.18f, 0.85f));
        private static readonly uint CloseHover = ImGui.ColorConvertFloat4ToU32(new Vector4(1.00f, 0.28f, 0.28f, 1.00f));
        private static readonly uint CloseRing = ImGui.ColorConvertFloat4ToU32(new Vector4(0.50f, 0.10f, 0.10f, 0.60f));

        public PetPopoutWindow(Plugin plugin)
            : base("###MementoPetPopout",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)
        {
            this.plugin = plugin;
            this.IsOpen = false;
            Size = new Vector2(PetW + FeedW + Pad * 3f, WinH);
            SizeCondition = ImGuiCond.Always;
        }

        public override void Dispose() { }

        private Vector4 ThemeAccent => plugin.Config.Theme switch
        {
            var t when t.StartsWith("Pretty in Pink") => MementoColors.Pink,
            var t when t.StartsWith("Glamourous Green") => MementoColors.Green,
            var t when t.StartsWith("Beautifully Blue") => new Vector4(0.28f, 0.60f, 1.00f, 1f),
            _ => MementoColors.Purple5,
        };

        public override void Draw()
        {
            if (Plugin.ObjectTable.LocalPlayer == null)
            {
                ImGui.TextColored(MementoColors.Dim, "Waiting for character data...");
                return;
            }

            var pet = plugin.PetManager.Pet;
            var dl = ImGui.GetWindowDrawList();
            var wp = ImGui.GetWindowPos();
            var ws = ImGui.GetWindowSize();

            dl.AddRectFilled(wp, wp + ws, MementoColors.ToU32(MementoColors.ScreenBg), 12f);
            dl.AddRect(wp, wp + ws, MementoColors.ToU32(ThemeAccent with { W = 0.55f }), 12f, ImDrawFlags.None, 1.8f);
            dl.AddRectFilled(wp, wp + new Vector2(ws.X, 3f),
                MementoColors.ToU32(ThemeAccent, 0.60f), 0f);

            float cr = 7f;
            var cc = new Vector2(wp.X + ws.X - cr - 8f, wp.Y + cr + 8f);
            bool hov = Vector2.Distance(ImGui.GetMousePos(), cc) <= cr + 2f;
            dl.AddCircleFilled(cc, cr + 1f, CloseRing);
            dl.AddCircleFilled(cc, cr, hov ? CloseHover : CloseNormal);
            float xo = 2.5f;
            uint xc = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, hov ? 0.90f : 0.55f));
            dl.AddLine(cc - new Vector2(xo, xo), cc + new Vector2(xo, xo), xc, 1.2f);
            dl.AddLine(cc + new Vector2(xo, -xo), cc + new Vector2(-xo, xo), xc, 1.2f);
            if (hov && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) IsOpen = false;

            DrawPetPanel(dl, wp, pet);
            float dx = wp.X + Pad + PetW + Pad * 0.5f;
            dl.AddLine(new Vector2(dx, wp.Y + 14f), new Vector2(dx, wp.Y + WinH - 14f),
                MementoColors.ToU32(ThemeAccent with { W = 0.55f }), 1f);
            DrawEventFeed(dl, wp);
        }

        private void DrawPetPanel(ImDrawListPtr dl, Vector2 wp, PetData? pet)
        {
            float px = wp.X + Pad, py = wp.Y + Pad + 4f;

            if (pet == null || !pet.IsAlive)
            {
                dl.AddText(new Vector2(px + 10f, py + WinH / 2f - 10f),
                    MementoColors.ToU32(MementoColors.Dim), "No pet yet...");
                ImGui.SetCursorPos(new Vector2(Pad, Pad));
                ImGui.Dummy(new Vector2(PetW, WinH - Pad * 2f));
                return;
            }

            dl.AddText(new Vector2(px, py),
                MementoColors.ToU32(ThemeAccent with { W = 0.90f }), pet.Name.ToUpper());
            dl.AddText(new Vector2(px, py + 16f),
                MementoColors.ToU32(MementoColors.Dim), PetManager.StageNames[(int)pet.Stage]);

            int mood = plugin.PetManager.GetMoodLevel();
            float fs = pet.Stage switch
            { PetStage.Egg => 3.8f, PetStage.Baby => 3.4f, PetStage.Teen => 3.0f, _ => 2.8f };
            float sw = 25f * fs, sh = 28f * fs;
            PetRenderer.Draw(pet,
                new Vector2(px + PetW / 2f - sw / 2f, py + 36f + (80f - sh) / 2f),
                fs, mood);

            string moodStr = plugin.PetManager.GetMoodString();
            var mSz = ImGui.CalcTextSize(moodStr);
            dl.AddText(new Vector2(px + PetW / 2f - mSz.X / 2f, py + 124f),
                MementoColors.ToU32(ThemeAccent, 0.85f), moodStr);

            float bt = py + 142f, bh = 18f, bw = PetW - 4f;
            EnzirionCore.UI.DrawHelpers.MiniStatBar(MementoColors.Palette, dl, px + 2f, bt, bw, "LV", pet.Love, MementoColors.Pink);
            EnzirionCore.UI.DrawHelpers.MiniStatBar(MementoColors.Palette, dl, px + 2f, bt + bh, bw, "JY", pet.Joy, MementoColors.Purple5);
            EnzirionCore.UI.DrawHelpers.MiniStatBar(MementoColors.Palette, dl, px + 2f, bt + bh * 2f, bw, "EN", pet.Energy, MementoColors.Amber);
            EnzirionCore.UI.DrawHelpers.MiniStatBar(MementoColors.Palette, dl, px + 2f, bt + bh * 3f, bw, "SP", pet.Spirit, MementoColors.Teal);

            // Vitality mini strip
            float vY = bt + bh * 4f + 2f;
            dl.AddRectFilled(new Vector2(px + 2f, vY), new Vector2(px + 2f + bw, vY + 3f),
                MementoColors.ToU32(MementoColors.Text, 0.06f), 2f);
            float vFill = Math.Clamp(pet.Vitality, 0f, 1f);
            if (vFill > 0f)
            {
                var vColor = vFill >= 0.60f ? MementoColors.Green
                           : vFill >= 0.30f ? MementoColors.Amber : MementoColors.Red;
                dl.AddRectFilled(new Vector2(px + 2f, vY),
                    new Vector2(px + 2f + bw * vFill, vY + 3f),
                    MementoColors.ToU32(vColor), 2f);
            }

            // Care strip
            float careY = vY + 6f;
            dl.AddRectFilled(new Vector2(px + 2f, careY), new Vector2(px + 2f + bw, careY + 3f),
                MementoColors.ToU32(MementoColors.Text, 0.06f), 2f);
            float careFill = Math.Clamp(pet.CareMeter / 100f, 0f, 1f);
            if (careFill > 0f)
                dl.AddRectFilled(new Vector2(px + 2f, careY),
                    new Vector2(px + 2f + bw * careFill, careY + 3f),
                    MementoColors.ToU32(MementoColors.GetCareColor(plugin.PetManager.GetCareLevel())), 2f);

            dl.AddText(new Vector2(px, careY + 6f),
                MementoColors.ToU32(MementoColors.Dim, 0.50f),
                $"⏱ {plugin.PetManager.GetLifespanString()}");

            ImGui.SetCursorPos(new Vector2(Pad, Pad));
            ImGui.Dummy(new Vector2(PetW, WinH - Pad * 2f));
        }

        private void DrawEventFeed(ImDrawListPtr dl, Vector2 wp)
        {
            float fx = wp.X + Pad + PetW + Pad;
            float fy = wp.Y + Pad + 4f;
            dl.AddText(new Vector2(fx, fy), MementoColors.ToU32(MementoColors.Dim), "RECENT EVENTS");

            float entH = 22f;
            int maxRows = (int)((WinH - Pad * 2f - 24f) / entH);

            ImGui.SetCursorPos(new Vector2(Pad + PetW + Pad, Pad + 24f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##evFeed", new Vector2(FeedW - Pad, WinH - Pad * 2f - 24f), false))
            {
                int count = 0;
                foreach (var entry in plugin.CharData.EventLog)
                {
                    if (count++ >= maxRows) break;
                    var parts = entry.Split('|');
                    if (parts.Length < 3) continue;
                    string icon = parts[0], desc = parts[1], time = parts[2];

                    // Color-coded dot per event type — no broken unicode icon text
                    Vector4 dotCol = icon switch
                    {
                        "♥" => MementoColors.Pink,
                        "✿" => MementoColors.Pink,
                        "⚔" => MementoColors.Amber,
                        "✦" => MementoColors.Teal,
                        "♪" => MementoColors.Purple5,
                        "!" => MementoColors.Green,
                        "⚑" => MementoColors.Teal,
                        "✕" => MementoColors.Red,
                        "▲" => MementoColors.Purple6,
                        _ => MementoColors.Dim,
                    };

                    var rp = ImGui.GetCursorScreenPos();
                    var rd = ImGui.GetWindowDrawList();

                    if (count % 2 == 0)
                        rd.AddRectFilled(rp, rp + new Vector2(FeedW - Pad, entH - 2f),
                            MementoColors.ToU32(MementoColors.Text, 0.025f), 3f);

                    rd.AddCircleFilled(rp + new Vector2(5f, entH / 2f - 2f), 3.5f,
                        MementoColors.ToU32(dotCol, 0.90f));

                    float baseY = rp.Y + (entH - ImGui.GetTextLineHeight()) / 2f - 2f;
                    rd.AddText(new Vector2(rp.X + 13f, baseY),
                        MementoColors.ToU32(MementoColors.Dim, 0.65f), time);

                    // Short-form for popout: strip stat deltas from text, render as colored dots
                    string shortDesc = desc;
                    string deltaStr = "";
                    int arrowIdx = shortDesc.IndexOf('→');
                    if (arrowIdx > 0)
                    {
                        string afterArrow = shortDesc[(arrowIdx + 1)..].Trim();
                        int spaceIdx = afterArrow.IndexOf(' ');
                        if (spaceIdx > 0)
                        {
                            shortDesc = shortDesc[..(arrowIdx + 1)] + " " + afterArrow[..spaceIdx];
                            deltaStr = afterArrow[(spaceIdx + 1)..].Trim();
                        }
                        else
                        {
                            shortDesc = shortDesc[..(arrowIdx + 1)] + " " + afterArrow;
                        }
                    }
                    else
                    {
                        // Care events: shorten and strip parenthetical
                        if (shortDesc.Contains('('))
                            shortDesc = shortDesc[..shortDesc.IndexOf('(')].Trim();
                        shortDesc = shortDesc
                            .Replace(" session ", " ")
                            .Replace("Crafted something ", "Craft ");
                    }

                    float timeW3 = ImGui.CalcTextSize(time).X;
                    float dotsSpace = string.IsNullOrEmpty(deltaStr) ? 0f : 50f;
                    float maxW3 = FeedW - Pad - 18f - timeW3 - dotsSpace;
                    string compact = shortDesc;
                    while (compact.Length > 4 && ImGui.CalcTextSize(compact).X > maxW3)
                        compact = compact[..^1];
                    if (compact.Length < shortDesc.Length) compact = compact[..^1] + "\u2026";

                    float textX = rp.X + 13f + timeW3 + 5f;
                    rd.AddText(new Vector2(textX, baseY),
                        MementoColors.ToU32(MementoColors.Text), compact);

                    // Render stat deltas as colored symbols
                    // New format: "♥♥♥ ★★ ✦ ⚡" — each char is one symbol
                    if (!string.IsNullOrEmpty(deltaStr))
                    {
                        float symX = textX + ImGui.CalcTextSize(compact).X + 4f;
                        float symY = baseY;
                        foreach (char ch in deltaStr)
                        {
                            if (ch == ' ') { symX += 2f; continue; }
                            Vector4 col = ch switch
                            {
                                '\u2665' => MementoColors.Pink,    // ♥ Love
                                '\u2605' => MementoColors.Purple5, // ★ Joy
                                '\u2726' => MementoColors.Teal,    // ✦ Spirit
                                '\u26A1' => MementoColors.Amber,   // ⚡ Energy
                                '\u2715' => MementoColors.Red,     // ✕ Negative
                                _ => MementoColors.Dim,
                            };
                            if (col.W < 0.01f) continue;
                            string s = ch.ToString();
                            rd.AddText(new Vector2(symX, symY),
                                MementoColors.ToU32(col, 0.90f), s);
                            symX += ImGui.CalcTextSize(s).X + 1f;
                        }
                    }

                    ImGui.Dummy(new Vector2(FeedW - Pad, entH - 2f));
                }

                if (count == 0)
                {
                    ImGui.Spacing();
                    float cw = ImGui.GetContentRegionAvail().X;
                    string msg = "No events yet...";
                    ImGui.SetCursorPosX((cw - ImGui.CalcTextSize(msg).X) / 2f);
                    ImGui.TextColored(MementoColors.Dim, msg);
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }
}
