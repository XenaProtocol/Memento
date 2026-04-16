using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Memento.Pet;

namespace Memento.Windows.Tabs
{
    /// <summary>
    /// Main pet view — sprite + mood, stat bars, play button, hatch animation,
    /// and bottom grid showing recent events + best friends cards.
    /// </summary>
    public class PetTab : IDrawablePage
    {
        private readonly Plugin plugin;
        public string TabLabel => " Pet ";

        // Hatch flow state — drives the Egg → Wobble → Crack → Reveal → Name sequence
        private HatchState hatchState = HatchState.None;
        private EggTier hatchTier = EggTier.Gold;
        private PetSpecies hatchSpecies = PetSpecies.Fox;
        private string nameInput = "";
        private float hatchTimer = 0f;

        public PetTab(Plugin plugin) { this.plugin = plugin; }

        public void Draw()
        {
            float pH = ImGui.GetContentRegionAvail().Y - 4f;
            var pet = plugin.PetManager.Pet;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##PetScroll", new Vector2(0, pH), false))
            {
                float W = ImGui.GetContentRegionAvail().X; // measured inside child = excludes scrollbar
                var dl0 = ImGui.GetWindowDrawList();
                dl0.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
                if (hatchState != HatchState.None)
                    DrawHatchingSequence(W);
                else if (pet == null || !pet.IsAlive)
                    DrawReadyToHatch(W);
                else if (pet.NamingPending)
                    DrawNamingScreen(W, pet);
                else
                {
                    ImGui.Spacing();
                    DrawPetScreen(pet, W);
                    ImGui.Spacing();
                    DrawCareMeterCard(pet, W);
                    ImGui.Spacing();
                    DrawBottomGrid(W);
                }
                ImGui.Dummy(new Vector2(W, 16f));
                dl0.PopClipRect();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        // ═════════════════════════════════════════════════════════════════
        // READY TO HATCH
        // ═════════════════════════════════════════════════════════════════
        private void DrawReadyToHatch(float W)
        {
            ImGui.Spacing(); ImGui.Spacing();
            var tier = plugin.PetManager.DetermineEggTier();
            var tierColor = tier switch
            {
                EggTier.Gold => MementoColors.Gold,
                EggTier.Silver => MementoColors.Dim,
                _ => MementoColors.Amber with { W = 0.7f },
            };
            string tierLabel = PetManager.TierNames[(int)tier];
            string oddsStr = tier switch
            {
                EggTier.Gold => "Fox 50%  ·  Bunny 25%  ·  Sparrow 25%",
                EggTier.Silver => "Bunny 50%  ·  Fox 25%  ·  Sparrow 25%",
                _ => "Sparrow 50%  ·  Fox 25%  ·  Bunny 25%",
            };

            CenterText($"You have a {tierLabel} Egg", tierColor, W);
            ImGui.Spacing();
            CenterText(oddsStr, MementoColors.Dim, W);
            ImGui.Spacing(); ImGui.Spacing();

            float t = (float)ImGui.GetTime();
            var dl = ImGui.GetWindowDrawList();
            var eggCenter = new Vector2(
                ImGui.GetCursorScreenPos().X + W / 2f + MathF.Sin(t * 2.5f) * 4f,
                ImGui.GetCursorScreenPos().Y + 55f);
            DrawEggAt(dl, eggCenter, 5f, tier);
            ImGui.Dummy(new Vector2(W, 115f));
            ImGui.Spacing(); ImGui.Spacing();

            float btnW = 200f;
            ImGui.SetCursorPosX((W - btnW) / 2f);
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Purple5 with { W = 0.25f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.45f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Text);
            if (ImGui.Button("  Hatch Egg!  ", new Vector2(btnW, 36f)))
            {
                var (t2, s) = plugin.PetManager.BeginHatch();
                hatchTier = t2; hatchSpecies = s;
                hatchState = HatchState.Wobbling; hatchTimer = 0f;
            }
            ImGui.PopStyleColor(3);
        }

        // ═════════════════════════════════════════════════════════════════
        // HATCHING SEQUENCE
        // ═════════════════════════════════════════════════════════════════
        private void DrawHatchingSequence(float W)
        {
            hatchTimer += ImGui.GetIO().DeltaTime;
            var dl = ImGui.GetWindowDrawList();
            ImGui.Spacing(); ImGui.Spacing();

            switch (hatchState)
            {
                case HatchState.Wobbling:
                    {
                        var c = new Vector2(
                            ImGui.GetCursorScreenPos().X + W / 2f + MathF.Sin(hatchTimer * 8f) * (6f + hatchTimer * 3f),
                            ImGui.GetCursorScreenPos().Y + 60f);
                        DrawEggAt(dl, c, 5.5f + MathF.Sin(hatchTimer * 4f) * 0.3f, hatchTier);
                        ImGui.Dummy(new Vector2(W, 130f));
                        CenterText("Something is stirring...", MementoColors.Dim, W);
                        if (hatchTimer > 2.5f) { hatchState = HatchState.Cracking; hatchTimer = 0f; }
                        break;
                    }
                case HatchState.Cracking:
                    {
                        var c = new Vector2(
                            ImGui.GetCursorScreenPos().X + W / 2f + MathF.Sin(hatchTimer * 14f) * 9f,
                            ImGui.GetCursorScreenPos().Y + 60f);
                        DrawEggAt(dl, c, 5.5f, hatchTier, cracked: true);
                        ImGui.Dummy(new Vector2(W, 130f));
                        CenterText("It's hatching!", MementoColors.Pink, W);
                        if (hatchTimer > 1.8f) { hatchState = HatchState.Revealing; hatchTimer = 0f; }
                        break;
                    }
                case HatchState.Revealing:
                    {
                        string sName = PetManager.SpeciesNames[(int)hatchSpecies];
                        CenterText($"A {sName} hatched!", MementoColors.Gold, W);
                        ImGui.Spacing();
                        CenterText($"From a {PetManager.TierNames[(int)hatchTier]} egg", MementoColors.Dim, W);
                        ImGui.Spacing(); ImGui.Spacing();
                        var fakePet = new PetData { Stage = PetStage.Egg, Species = hatchSpecies, IsAlive = true };
                        PetRenderer.Draw(fakePet,
                            new Vector2(ImGui.GetCursorScreenPos().X + W / 2f - 48f,
                                        ImGui.GetCursorScreenPos().Y + 10f), 4.5f, 0);
                        ImGui.Dummy(new Vector2(W, 110f));
                        if (hatchTimer > 2f) { hatchState = HatchState.Naming; hatchTimer = 0f; }
                        break;
                    }
                case HatchState.Naming:
                    {
                        string sName = PetManager.SpeciesNames[(int)hatchSpecies];
                        CenterText($"What will you name your {sName}?", MementoColors.Purple5, W);
                        ImGui.Spacing(); ImGui.Spacing();
                        var fakePet = new PetData { Stage = PetStage.Egg, Species = hatchSpecies, IsAlive = true };
                        PetRenderer.Draw(fakePet,
                            new Vector2(ImGui.GetCursorScreenPos().X + W / 2f - 48f,
                                        ImGui.GetCursorScreenPos().Y + 10f), 4.0f, 0);
                        ImGui.Dummy(new Vector2(W, 100f));
                        ImGui.Spacing();
                        float inputW = 240f;
                        ImGui.SetCursorPosX((W - inputW - 110f) / 2f);
                        ImGui.SetNextItemWidth(inputW);
                        ImGui.InputTextWithHint("##PetName", $"{sName}...", ref nameInput, 32);
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Purple5 with { W = 0.30f });
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.50f });
                        ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Text);
                        if (ImGui.Button("Confirm!", new Vector2(100f, 24f)))
                        { plugin.PetManager.FinaliseName(nameInput); hatchState = HatchState.None; nameInput = ""; }
                        ImGui.PopStyleColor(3);
                        ImGui.Spacing();
                        CenterText("Leave blank to use species name", MementoColors.Dim with { W = 0.5f }, W);
                        break;
                    }
            }
        }

        private void DrawNamingScreen(float W, PetData pet)
        {
            ImGui.Spacing(); ImGui.Spacing();
            CenterText("Give your pet a name!", MementoColors.Purple5, W);
            ImGui.Spacing(); ImGui.Spacing();
            float inputW = 240f;
            ImGui.SetCursorPosX((W - inputW - 110f) / 2f);
            ImGui.SetNextItemWidth(inputW);
            ImGui.InputTextWithHint("##PetNameFix", $"{PetManager.SpeciesNames[(int)pet.Species]}...", ref nameInput, 32);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Purple5 with { W = 0.30f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.50f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Text);
            if (ImGui.Button("Confirm!##fix", new Vector2(100f, 24f)))
            { plugin.PetManager.FinaliseName(nameInput); nameInput = ""; }
            ImGui.PopStyleColor(3);
        }

        // ═════════════════════════════════════════════════════════════════
        // PET SCREEN CARD
        // ═════════════════════════════════════════════════════════════════
        private void DrawPetScreen(PetData pet, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var o = ImGui.GetCursorScreenPos();
            float pad = 14f;

            // Heights
            float headerH = 66f;
            float foxH = 110f;
            float moodH = 28f;
            float statsH = 58f;  // 2 rows of 2 bars
            float vitalH = 42f;  // label row + bar row
            float footerH = 22f;
            float totalH = headerH + foxH + moodH + statsH + vitalH + footerH + pad * 2f;

            dl.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
            dl.AddRectFilled(o, o + new Vector2(W, totalH), MementoColors.ToU32(MementoColors.ScreenBg), 12f);
            dl.AddRect(o, o + new Vector2(W, totalH), MementoColors.ToU32(MementoColors.Border), 12f, ImDrawFlags.None, 1.5f);
            ImGui.Dummy(new Vector2(W, totalH));

            // Stage pills
            DrawStagePills(dl, o + new Vector2(pad, pad), pet.Stage);

            // Name + meta
            float nameY = o.Y + pad + 24f;
            dl.AddText(new Vector2(o.X + pad, nameY),
                MementoColors.ToU32(MementoColors.Purple5), pet.Name.ToUpper());
            dl.AddText(new Vector2(o.X + pad, nameY + 16f),
                MementoColors.ToU32(MementoColors.Dim),
                $"Day {(int)(DateTime.Now - pet.HatchedAt).TotalDays + 1}  ·  {PetManager.StageNames[(int)pet.Stage]}  ·  {PetManager.SpeciesNames[(int)pet.Species]}");

            // Fox sprite with mood
            int mood = plugin.PetManager.GetMoodLevel();
            float fs = pet.Stage switch
            { PetStage.Egg => 4.2f, PetStage.Baby => 3.8f, PetStage.Teen => 3.4f, _ => 3.1f };
            PetRenderer.Draw(pet,
                new Vector2(o.X + W / 2f - 25f * fs / 2f,
                            o.Y + headerH + foxH / 2f - 28f * fs / 2f),
                fs, mood);

            // Mood pill
            string moodTxt = $"  {plugin.PetManager.GetMoodIcon()}  {plugin.PetManager.GetMoodString()}  ";
            var mSz = ImGui.CalcTextSize(moodTxt);
            float mCX = o.X + W / 2f, mY = o.Y + headerH + foxH + (moodH - mSz.Y) / 2f;
            var bTL = new Vector2(mCX - mSz.X / 2f - 6f, mY - 3f);
            var bBR = new Vector2(mCX + mSz.X / 2f + 6f, mY + mSz.Y + 3f);
            dl.AddRectFilled(bTL, bBR, MementoColors.ToU32(MementoColors.Purple5, 0.18f), 12f);
            dl.AddRect(bTL, bBR, MementoColors.ToU32(MementoColors.Purple5, 0.35f), 12f);
            dl.AddText(new Vector2(mCX - mSz.X / 2f, mY), MementoColors.ToU32(MementoColors.Purple5), moodTxt);

            // 4 stat bars in 2×2 grid
            float bTop = o.Y + headerH + foxH + moodH + 4f;
            float rowH = 26f;
            float halfW = (W - pad * 3f) / 2f;
            float col2 = o.X + pad + halfW + pad;
            float lW = 52f;
            EnzirionCore.UI.DrawHelpers.StatBar(MementoColors.Palette, dl, o.X + pad, bTop, halfW, "LOVE", pet.Love, lW, MementoColors.Pink);
            EnzirionCore.UI.DrawHelpers.StatBar(MementoColors.Palette, dl, col2, bTop, halfW, "JOY", pet.Joy, lW, MementoColors.JoyPurple);
            EnzirionCore.UI.DrawHelpers.StatBar(MementoColors.Palette, dl, o.X + pad, bTop + rowH, halfW, "ENERGY", pet.Energy, lW, MementoColors.Amber);
            EnzirionCore.UI.DrawHelpers.StatBar(MementoColors.Palette, dl, col2, bTop + rowH, halfW, "SPIRIT", pet.Spirit, lW, MementoColors.Teal);

            // Vitality bar — label centered above, full-width bar below
            float vY = bTop + rowH * 2f + 6f;
            float vFill = Math.Clamp(pet.Vitality, 0f, 1f);
            var vColor = vFill >= 0.60f ? MementoColors.Green
                        : vFill >= 0.30f ? MementoColors.Amber
                        : MementoColors.Red;
            float lh2 = ImGui.GetTextLineHeight();

            // Label row: "OVERALL HEALTH" left, pct right
            string vLabel = "OVERALL HEALTH";
            string vPctTxt = $"{(int)(vFill * 100f)}%";
            dl.AddText(new Vector2(o.X + pad, vY),
                MementoColors.ToU32(MementoColors.Dim), vLabel);
            var vPctSz = ImGui.CalcTextSize(vPctTxt);
            dl.AddText(new Vector2(o.X + W - pad - vPctSz.X, vY),
                MementoColors.ToU32(vColor), vPctTxt);

            // Bar row: full width below the label
            float vBarY = vY + lh2 + 3f;
            float vBarX = o.X + pad;
            float vBarW = W - pad * 2f;
            dl.AddRectFilled(new Vector2(vBarX, vBarY),
                new Vector2(vBarX + vBarW, vBarY + 8f),
                MementoColors.ToU32(MementoColors.Text, 0.06f), 4f);
            if (vFill > 0f)
                dl.AddRectFilled(new Vector2(vBarX, vBarY),
                    new Vector2(vBarX + vBarW * vFill, vBarY + 8f),
                    MementoColors.ToU32(vColor), 4f);

            // Footer
            float footY = o.Y + totalH - footerH - 4f;
            dl.AddText(new Vector2(o.X + pad, footY),
                MementoColors.ToU32(MementoColors.Dim),
                $"Alive for  {plugin.PetManager.GetLifespanString()}");

            int toNext = plugin.PetManager.EmotesUntilNextStage();
            if (toNext > 0 && pet.Stage != PetStage.Adult)
            {
                string hint = $"{toNext} more emotes to grow!";
                dl.AddText(
                    new Vector2(o.X + W / 2f - ImGui.CalcTextSize(hint).X / 2f, footY),
                    MementoColors.ToU32(MementoColors.Purple5, 0.55f), hint);
            }

            // Sparkle icons
            float t2 = (float)ImGui.GetTime();
            var spk = new Vector2(o.X + W - 54f, footY);
            dl.AddText(spk, MementoColors.ToU32(MementoColors.Gold, (MathF.Sin(t2 * 2f) + 1f) / 2f), "✦");
            dl.AddText(spk + new Vector2(18f, 0f), MementoColors.ToU32(MementoColors.Pink, (MathF.Sin(t2 * 2f + 1.4f) + 1f) / 2f), "♥");
            dl.AddText(spk + new Vector2(36f, 0f), MementoColors.ToU32(MementoColors.Purple6, (MathF.Sin(t2 * 2f + 2.8f) + 1f) / 2f), "✦");
            dl.PopClipRect();
        }

        // ═════════════════════════════════════════════════════════════════
        // CARE METER CARD  (simple bar + 2 buttons)
        // ═════════════════════════════════════════════════════════════════
        private void DrawCareMeterCard(PetData pet, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var o = ImGui.GetCursorScreenPos();
            var localO = ImGui.GetCursorPos();
            float pad = 14f;
            float cardH = 108f;
            bool canPlay = plugin.PetManager.CanPlayNow();
            var careColor = MementoColors.GetCareColor(plugin.PetManager.GetCareLevel());

            dl.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
            dl.AddRectFilled(o, o + new Vector2(W, cardH), MementoColors.ToU32(MementoColors.Panel2), 10f);
            dl.AddRect(o, o + new Vector2(W, cardH), MementoColors.ToU32(MementoColors.Border), 10f, ImDrawFlags.None, 1f);
            ImGui.Dummy(new Vector2(W, cardH));

            // Header
            dl.AddText(o + new Vector2(pad, 10f), MementoColors.ToU32(MementoColors.Dim), "DAILY CARE");
            dl.AddText(o + new Vector2(pad, 26f), MementoColors.ToU32(careColor),
                plugin.PetManager.CareStatusString());

            // Bar
            float barX = o.X + pad, barY = o.Y + 46f, barW = W - pad * 2f;
            float fill = Math.Clamp(pet.CareMeter / 100f, 0f, 1f);
            dl.AddRectFilled(new Vector2(barX, barY), new Vector2(barX + barW, barY + 8f),
                MementoColors.ToU32(MementoColors.Text, 0.06f), 5f);
            if (fill > 0f)
                dl.AddRectFilled(new Vector2(barX, barY),
                    new Vector2(barX + barW * fill, barY + 8f),
                    MementoColors.ToU32(careColor), 5f);

            // 50% marker
            float mkX = barX + barW * 0.5f;
            dl.AddLine(new Vector2(mkX, barY - 2f), new Vector2(mkX, barY + 10f),
                MementoColors.ToU32(MementoColors.Amber, 0.65f), 1.5f);
            dl.AddText(new Vector2(mkX - 12f, barY + 12f),
                MementoColors.ToU32(MementoColors.Amber, 0.50f), "50%");
            dl.AddText(new Vector2(barX + barW - 30f, barY - 1f),
                MementoColors.ToU32(careColor), $"{(int)pet.CareMeter}%");

            if (pet.NeglectStreakDays > 0)
                dl.AddText(new Vector2(barX, barY + 26f),
                    MementoColors.ToU32(MementoColors.Red, 0.80f),
                    $"⚠ {pet.NeglectStreakDays} neglect day{(pet.NeglectStreakDays > 1 ? "s" : "")}");

            // 2 buttons below bar
            float btnY = o.Y + cardH - 34f;
            float btnW = (W - pad * 2f - 8f) / 2f;
            float btn1X = o.X + pad;
            float btn2X = btn1X + btnW + 8f;

            // Play button
            DrawCareButton(dl, btn1X, btnY, btnW, 28f,
                canPlay ? "♪  Play with pet" : $"♪  Ready in {plugin.PetManager.PlayCooldownString()}",
                canPlay ? MementoColors.Purple5 : MementoColors.Dim,
                canPlay);
            dl.PopClipRect();

            ImGui.SetCursorScreenPos(new Vector2(btn1X, btnY));
            if (ImGui.InvisibleButton("##playBtn", new Vector2(btnW, 28f)) && canPlay)
                plugin.TryPlayWithPet();

            // Checklist button
            dl.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
            DrawCareButton(dl, btn2X, btnY, btnW, 28f,
                "☑  Open daily checklist  ↗",
                MementoColors.Teal, true);
            dl.PopClipRect();

            ImGui.SetCursorScreenPos(new Vector2(btn2X, btnY));
            if (ImGui.InvisibleButton("##checklistBtn", new Vector2(btnW, 28f)))
                plugin.OpenChecklist();

            ImGui.SetCursorPos(new Vector2(localO.X, localO.Y + cardH));
        }

        private void DrawCareButton(ImDrawListPtr dl, float x, float y,
            float w, float h, string label, Vector4 color, bool active)
        {
            var tl = new Vector2(x, y); var br = new Vector2(x + w, y + h);
            dl.AddRectFilled(tl, br, MementoColors.ToU32(active
                ? color with { W = 0.18f }
                : MementoColors.Text with { W = 0.04f }), 7f);
            dl.AddRect(tl, br, MementoColors.ToU32(active
                ? color with { W = 0.55f }
                : MementoColors.Dim with { W = 0.20f }), 7f, ImDrawFlags.None, 1f);
            var lSz = ImGui.CalcTextSize(label);
            dl.AddText(tl + new Vector2((w - lSz.X) / 2f, (h - lSz.Y) / 2f),
                MementoColors.ToU32(active ? color : MementoColors.Dim), label);
        }

        // ═════════════════════════════════════════════════════════════════
        // BOTTOM GRID: Recent Events (2/3) | Best Friends (1/3)
        // ═════════════════════════════════════════════════════════════════
        private void DrawBottomGrid(float W)
        {
            float gap = 16f;
            float evtW = MathF.Floor((W - gap) * 0.65f);
            float friendW = W - evtW - gap;
            float cardH = 168f;

            // Save positions and place both cards explicitly via screen coords
            var startScreen = ImGui.GetCursorScreenPos();
            var startLocal = ImGui.GetCursorPos();

            DrawEventsCard(evtW, cardH);

            // Place friends card to the right of events card at the SAME vertical position
            ImGui.SetCursorScreenPos(new Vector2(startScreen.X + evtW + gap, startScreen.Y));
            DrawFriendsCard(friendW, cardH);

            // Advance cursor past both cards
            ImGui.SetCursorPos(new Vector2(startLocal.X, startLocal.Y + cardH));
        }

        private void DrawEventsCard(float w, float h)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var localPos = ImGui.GetCursorPos();

            // Reserve full card space upfront so SameLine works in DrawBottomGrid
            ImGui.Dummy(new Vector2(w, h));

            // Draw card background
            dl.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
            dl.AddRectFilled(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Panel2), 8f);
            dl.AddRect(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Border), 8f, ImDrawFlags.None, 1f);
            dl.AddText(pos + new Vector2(10f, 8f), MementoColors.ToU32(MementoColors.Dim), "RECENT EVENTS");
            dl.PopClipRect();
            float innerY = pos.Y + 28f;
            float innerH = h - 36f;
            float innerW = w - 20f;

            // Position inner child using screen coords (cursor already advanced by Dummy)
            ImGui.SetCursorScreenPos(new Vector2(pos.X + 10f, pos.Y + 28f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##evtChild", new Vector2(innerW, innerH), false, ImGuiWindowFlags.NoNav))
            {
                if (!plugin.CharData.EventLog.Any())
                {
                    ImGui.TextColored(MementoColors.Dim, "No events yet...");
                }
                else
                {
                    float rowH2 = ImGui.GetTextLineHeight() + 6f;
                    var dl2 = ImGui.GetWindowDrawList();
                    var clipMin2 = ImGui.GetWindowPos();
                    var clipMax2 = clipMin2 + ImGui.GetWindowSize();
                    dl2.PushClipRect(clipMin2, clipMax2, true);
                    int row = 0;

                    foreach (var entry in plugin.CharData.EventLog.Take(7))
                    {
                        var parts = entry.Split('|');
                        if (parts.Length < 3) continue;
                        string icon = parts[0], desc = parts[1], time = parts[2];

                        Vector4 dotCol = icon switch
                        {
                            "♥" => MementoColors.Pink,
                            "✿" => MementoColors.Pink,
                            "⚔" => MementoColors.Amber,
                            "✦" => MementoColors.Teal,
                            "♪" => MementoColors.JoyPurple,
                            "!" => MementoColors.Green,
                            "⚑" => MementoColors.Teal,
                            "✕" => MementoColors.Red,
                            "▲" => MementoColors.Purple6,
                            _ => MementoColors.Dim,
                        };

                        var rp = ImGui.GetCursorScreenPos();
                        // Zebra
                        if (row % 2 == 0)
                            dl2.AddRectFilled(rp, rp + new Vector2(innerW, rowH2),
                                MementoColors.ToU32(MementoColors.Text, 0.025f), 3f);

                        dl2.AddCircleFilled(rp + new Vector2(5f, rowH2 / 2f), 3.5f,
                            MementoColors.ToU32(dotCol, 0.90f));

                        float baseY = rp.Y + (rowH2 - ImGui.GetTextLineHeight()) / 2f;
                        dl2.AddText(new Vector2(rp.X + 13f, baseY),
                            MementoColors.ToU32(MementoColors.Dim, 0.70f), time);

                        float timeW = ImGui.CalcTextSize(time).X;
                        float maxW = innerW - 18f - timeW - 4f;
                        string d = desc;
                        while (d.Length > 4 && ImGui.CalcTextSize(d).X > maxW)
                            d = d[..^1];
                        if (d.Length < desc.Length) d = d[..^1] + "…";

                        dl2.AddText(new Vector2(rp.X + 13f + timeW + 5f, baseY),
                            MementoColors.ToU32(MementoColors.Text), d);

                        ImGui.Dummy(new Vector2(innerW, rowH2));
                        row++;
                    }
                    dl2.PopClipRect();
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawFriendsCard(float w, float h)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();

            // Reserve full card space so SameLine alignment is preserved
            ImGui.Dummy(new Vector2(w, h));

            // Draw card background
            dl.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
            dl.AddRectFilled(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Panel2), 8f);
            dl.AddRect(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Border), 8f, ImDrawFlags.None, 1f);
            dl.AddText(pos + new Vector2(10f, 8f), MementoColors.ToU32(MementoColors.Dim), "BEST FRIENDS");
            dl.PopClipRect();

            float innerW = w - 20f;
            float innerH = h - 36f;

            // Position inner child using screen coords
            ImGui.SetCursorScreenPos(new Vector2(pos.X + 10f, pos.Y + 28f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##friendChild", new Vector2(innerW, innerH), false, ImGuiWindowFlags.NoNav))
            {
                if (!plugin.CharData.AdmirerCounts.Any())
                {
                    ImGui.TextColored(MementoColors.Dim, "No friends yet...");
                }
                else
                {
                    float rowH2 = ImGui.GetTextLineHeight() + 6f;
                    var dl2 = ImGui.GetWindowDrawList();
                    var clipMin2 = ImGui.GetWindowPos();
                    var clipMax2 = clipMin2 + ImGui.GetWindowSize();
                    dl2.PushClipRect(clipMin2, clipMax2, true);
                    int rank = 0;

                    foreach (var kv in plugin.CharData.AdmirerCounts.OrderByDescending(x => x.Value).Take(5))
                    {
                        rank++;
                        var rp = ImGui.GetCursorScreenPos();

                        // Zebra
                        if (rank % 2 == 0)
                            dl2.AddRectFilled(rp, rp + new Vector2(innerW, rowH2),
                                MementoColors.ToU32(MementoColors.Text, 0.025f), 3f);

                        float baseY = rp.Y + (rowH2 - ImGui.GetTextLineHeight()) / 2f;
                        string rankStr = rank == 1 ? "♛" : $"#{rank}";
                        Vector4 rankCol = rank == 1 ? MementoColors.Gold : MementoColors.Dim;
                        dl2.AddText(new Vector2(rp.X, baseY), MementoColors.ToU32(rankCol), rankStr);

                        float rankW = ImGui.CalcTextSize(rankStr).X + 5f;
                        string fn = kv.Key.Contains(' ') ? kv.Key.Split(' ')[0] : kv.Key;
                        // Truncate name to fit
                        float maxNW = innerW - rankW - 28f;
                        while (fn.Length > 2 && ImGui.CalcTextSize(fn).X > maxNW) fn = fn[..^1];
                        dl2.AddText(new Vector2(rp.X + rankW, baseY),
                            MementoColors.ToU32(MementoColors.Purple5), fn);

                        // Count right-aligned
                        string cnt = kv.Value.ToString();
                        var cntSz = ImGui.CalcTextSize(cnt);
                        dl2.AddText(new Vector2(rp.X + innerW - cntSz.X, baseY),
                            MementoColors.ToU32(MementoColors.Dim), cnt);

                        ImGui.Dummy(new Vector2(innerW, rowH2));
                    }
                    dl2.PopClipRect();
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
        // ═════════════════════════════════════════════════════════════════
        private void DrawStagePills(ImDrawListPtr dl, Vector2 start, PetStage current)
        {
            string[] stages = { "Egg", "Baby", "Teen", "Adult" };
            float x = start.X;
            for (int i = 0; i < stages.Length; i++)
            {
                bool isActive = (int)current == i;
                bool isDone = (int)current > i;
                Vector4 bg, fg, border;
                if (isActive) { bg = MementoColors.Purple5 with { W = 0.25f }; fg = MementoColors.Purple5; border = MementoColors.Purple5 with { W = 0.70f }; }
                else if (isDone) { bg = MementoColors.Text with { W = 0.06f }; fg = MementoColors.Dim; border = MementoColors.Text with { W = 0.10f }; }
                else { bg = Vector4.Zero; fg = MementoColors.Text with { W = 0.18f }; border = MementoColors.Text with { W = 0.12f }; }
                string txt = stages[i].ToUpper();
                var sz = ImGui.CalcTextSize(txt);
                float pw = sz.X + 14f, ph = sz.Y + 6f;
                var tl = new Vector2(x, start.Y); var br = new Vector2(x + pw, start.Y + ph);
                dl.AddRectFilled(tl, br, MementoColors.ToU32(bg), 10f);
                dl.AddRect(tl, br, MementoColors.ToU32(border), 10f, ImDrawFlags.None, 0.8f);
                dl.AddText(tl + new Vector2(7f, 3f), MementoColors.ToU32(fg), txt);
                x += pw + 5f;
            }
        }

        private void DrawEggAt(ImDrawListPtr dl, Vector2 center, float scale,
            EggTier tier, bool cracked = false)
        {
            uint shell = tier switch
            {
                EggTier.Gold => ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.88f, 0.40f, 1f)),
                EggTier.Silver => ImGui.ColorConvertFloat4ToU32(new Vector4(0.82f, 0.82f, 0.88f, 1f)),
                _ => ImGui.ColorConvertFloat4ToU32(new Vector4(0.88f, 0.72f, 0.50f, 1f)),
            };
            uint spot = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.10f));
            uint crack = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.2f, 0.1f, 0.8f));
            float w = 14f * scale, h = 18f * scale;
            var tl = center - new Vector2(w / 2f, h / 2f);
            dl.AddRectFilled(tl + new Vector2(2 * scale, 0), tl + new Vector2(w - 2 * scale, h), shell, 8f * scale);
            dl.AddRectFilled(tl + new Vector2(0, 4 * scale), tl + new Vector2(w, h - 4 * scale), shell, 4f * scale);
            dl.AddCircleFilled(tl + new Vector2(w * 0.35f, h * 0.35f), 2.5f * scale, spot);
            dl.AddCircleFilled(tl + new Vector2(w * 0.65f, h * 0.55f), 1.8f * scale, spot);
            if (cracked)
            {
                dl.AddLine(center + new Vector2(-1, -6) * scale, center + new Vector2(3, 0) * scale, crack, 1.5f);
                dl.AddLine(center + new Vector2(3, 0) * scale, center + new Vector2(-2, 4) * scale, crack, 1.5f);
            }
        }

        private static void CenterText(string text, Vector4 color, float W)
        {
            ImGui.SetCursorPosX((W - ImGui.CalcTextSize(text).X) / 2f);
            ImGui.TextColored(color, text);
        }
    }
}
