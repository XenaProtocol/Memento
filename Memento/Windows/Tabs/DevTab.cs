using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Memento.Pet;

namespace Memento.Windows.Tabs
{
    /// <summary>
    /// Dev/sandbox tab — shadow pet for testing, stat sliders, emote simulator,
    /// care controls, interactive console, and full backup import/export.
    /// All sandbox actions operate on an in-memory pet, never the real one.
    /// </summary>
    public class DevTab : IDrawablePage
    {
        private readonly Plugin plugin;
        public string TabLabel => " Dev ";
        private readonly List<string> devLog = new();

        private bool sandboxOn = false;
        private bool dangerousEnabled = false;
        private string importTokenBuf = "";
        private string backupFeedback = "";
        private float backupFeedbackTimer = 0f;
        private string? pendingConfirmAction = null;
        private PetData? shadowPet = null;

        private string fakePlayer = "DevTester";
        private string fakeEmote = "Dote";
        private float setCare = 50f;
        private float setLove = 0.5f, setJoy = 0.5f, setEnergy = 0.5f, setSpirit = 0.5f;
        private bool slidersDirty = false;

        private string commandInput = "";

        private static readonly string[] QuickEmotes =
            { "Dote","Embrace","Hug","Comfort","Cheer","Flower Shower","Furious","Slap","Poke","Blow Kiss" };

        public DevTab(Plugin plugin) { this.plugin = plugin; }

        private void Log(string msg, bool warn = false)
        {
            devLog.Add($"[{DateTime.Now:HH:mm:ss}] {(warn ? "! " : "> ")}{msg}");
            if (devLog.Count > 80) devLog.RemoveAt(0);
        }

        private void EnsureShadowPet()
        {
            if (shadowPet != null) return;
            shadowPet = new PetData
            {
                Name = "TestPet", Species = PetSpecies.Fox, Tier = EggTier.Gold,
                Stage = PetStage.Egg, IsAlive = true, NamingPending = false,
                HatchedAt = DateTime.Now, LastDecayCheck = DateTime.Now,
                LastCareReset = DateTime.Today, BonusEmote = "Dote",
                Love = 0.5f, Joy = 0.5f, Spirit = 0.6f, Energy = 0.6f,
            };
            setLove = shadowPet.Love; setJoy = shadowPet.Joy;
            setEnergy = shadowPet.Energy; setSpirit = shadowPet.Spirit;
            setCare = shadowPet.CareMeter;
            Log("Shadow pet created (Fox, Gold egg, Fav: Dote)");
        }

        public void Draw()
        {
            float pH = ImGui.GetContentRegionAvail().Y - 4f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##DevOuter", new Vector2(0, pH), false))
            {
                float W = ImGui.GetContentRegionAvail().X;
                var dl = ImGui.GetWindowDrawList();
                dl.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
                ImGui.Spacing();

                // Warning banner
                var bannerBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.08f, 0.08f, 0.45f));
                var bp = ImGui.GetCursorScreenPos();
                dl.AddRectFilled(bp, bp + new Vector2(W, 30f), bannerBg, 6f);
                dl.AddText(bp + new Vector2(10f, 7f),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.65f, 0.45f, 1f)),
                    "Dev / Sandbox — shadow pet is separate from your real pet");
                ImGui.Dummy(new Vector2(W, 32f));

                // Sandbox toggle
                ImGui.TextColored(MementoColors.Dim, "Sandbox:");
                ImGui.SameLine();
                if (ImGui.Checkbox("##sb", ref sandboxOn))
                {
                    if (sandboxOn) EnsureShadowPet();
                }
                ImGui.SameLine();
                ImGui.TextColored(sandboxOn ? MementoColors.Green : MementoColors.Dim,
                    sandboxOn ? "ON — controls act on shadow pet only"
                              : "OFF — enable to use dev controls");
                ImGui.Spacing();

                if (!sandboxOn)
                {
                    DrawReadOnly(W);
                }
                else
                {
                    EnsureShadowPet();
                    DrawSandbox(shadowPet!, W);
                }

                ImGui.Dummy(new Vector2(W, 16f));
                dl.PopClipRect();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawReadOnly(float W)
        {
            SectionTitle("Real Pet (read-only)", W);
            ImGui.Spacing();
            var real = plugin.PetManager.Pet;
            if (real != null && real.IsAlive && !real.NamingPending)
            {
                ImGui.TextColored(MementoColors.Dim, "Name:"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Purple5, real.Name);
                ImGui.SameLine(0, 16f);
                ImGui.TextColored(MementoColors.Dim, "Stage:"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Teal, PetManager.StageNames[(int)real.Stage]);
                ImGui.SameLine(0, 16f);
                ImGui.TextColored(MementoColors.Dim, "Emotes:"); ImGui.SameLine(); ImGui.TextColored(MementoColors.Purple5, real.TotalEmotesReceived.ToString());

                ImGui.TextColored(MementoColors.Pink, $"LV {(int)(real.Love * 100)}%"); ImGui.SameLine(90f);
                ImGui.TextColored(MementoColors.JoyPurple, $"JY {(int)(real.Joy * 100)}%"); ImGui.SameLine(180f);
                ImGui.TextColored(MementoColors.Amber, $"EN {(int)(real.Energy * 100)}%"); ImGui.SameLine(270f);
                ImGui.TextColored(MementoColors.Teal, $"SP {(int)(real.Spirit * 100)}%");
                ImGui.TextColored(MementoColors.Green, $"Vitality {(int)(real.Vitality * 100)}%"); ImGui.SameLine(160f);
                ImGui.TextColored(MementoColors.Amber, $"Care {(int)real.CareMeter}%"); ImGui.SameLine(260f);
                ImGui.TextColored(MementoColors.Red, $"Neglect {real.NeglectStreakDays}d");
            }
            else ImGui.TextColored(MementoColors.Dim, "No active pet.");

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Data Reset (Real Config)", W);
            ImGui.Spacing();

            // Dangerous actions guard
            ImGui.TextColored(MementoColors.Red with { W = 0.80f }, "Dangerous Actions:");
            ImGui.SameLine();
            if (ImGui.Checkbox("##danger", ref dangerousEnabled))
            { if (!dangerousEnabled) pendingConfirmAction = null; }
            ImGui.SameLine();
            ImGui.TextColored(dangerousEnabled ? MementoColors.Red : MementoColors.Dim with { W = 0.55f },
                dangerousEnabled ? "ENABLED — destructive buttons active!" : "Disabled — tick to unlock destructive actions");
            ImGui.Spacing();

            using (new DisabledScope(!dangerousEnabled))
            {
                PushRedBtn();
                if (ImGui.Button("Clear Social##ro", new Vector2(130f, 24f)) && dangerousEnabled)
                    pendingConfirmAction = "Clear Social";
                ImGui.SameLine(0, 6f);
                if (ImGui.Button("Clear Events##ro", new Vector2(130f, 24f)) && dangerousEnabled)
                    pendingConfirmAction = "Clear Events";
                ImGui.SameLine(0, 6f);
                if (ImGui.Button("Clear Dev Log##ro", new Vector2(130f, 24f)) && dangerousEnabled)
                { devLog.Clear(); Log("Dev log cleared"); }
                ImGui.PopStyleColor(3);
            }

            // Confirmation popup
            DrawConfirmPopup(W);

            ImGui.Spacing(); ImGui.Spacing();

            // ── Full Backup ───────────────────────────────────────────────
            SectionTitle("Full Backup Token", W);
            ImGui.Spacing();
            ImGui.TextColored(MementoColors.Dim with { W = 0.70f },
                "Exports everything: pet, social, emote counts, observer history, settings, unlocks.");
            ImGui.Spacing();

            if (backupFeedbackTimer > 0f) backupFeedbackTimer -= ImGui.GetIO().DeltaTime;
            if (backupFeedbackTimer <= 0f) backupFeedback = "";

            PushGreenBtn();
            if (ImGui.Button("Export Full Backup##exp", new Vector2(170f, 26f)))
            {
                string tok = ExportFullBackup();
                ImGui.SetClipboardText(tok);
                backupFeedback = "✓ Copied to clipboard!";
                backupFeedbackTimer = 4f;
                Log($"Full backup exported ({tok.Length} chars)");
            }
            ImGui.PopStyleColor(3);
            if (!string.IsNullOrEmpty(backupFeedback) && backupFeedback.StartsWith("✓"))
            { ImGui.SameLine(0, 10f); ImGui.TextColored(MementoColors.Green, backupFeedback); }

            ImGui.Spacing();
            ImGui.TextColored(MementoColors.Dim with { W = 0.60f }, "Paste a backup token to restore:");
            ImGui.SetNextItemWidth(W - 110f);
            ImGui.InputText("##importbak", ref importTokenBuf, 8192);
            ImGui.SameLine(0, 8f);
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Teal with { W = 0.20f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Teal with { W = 0.38f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Text);
            if (ImGui.Button("Import##bakimp", new Vector2(90f, 26f)))
            {
                backupFeedback = ImportFullBackup(importTokenBuf.Trim());
                backupFeedbackTimer = 5f;
                importTokenBuf = "";
                Log(backupFeedback);
            }
            ImGui.PopStyleColor(3);
            if (!string.IsNullOrEmpty(backupFeedback))
            {
                ImGui.Spacing();
                ImGui.TextColored(backupFeedback.StartsWith("✓") ? MementoColors.Green : MementoColors.Red,
                    backupFeedback);
            }

            ImGui.Spacing(); ImGui.Spacing();
            DrawDevConsole(W);
        }

        private void DrawConfirmPopup(float W)
        {
            if (pendingConfirmAction == null) return;

            ImGui.OpenPopup("##devConfirm");
            ImGui.SetNextWindowSize(new Vector2(320f, 140f), ImGuiCond.Always);
            bool modalOpen = true;
            if (ImGui.BeginPopupModal("##devConfirm", ref modalOpen,
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                string action = pendingConfirmAction ?? "";
                ImGui.Spacing();
                ImGui.SetCursorPosX(12f);
                ImGui.TextColored(MementoColors.Red, $"Are you sure?");
                ImGui.SetCursorPosX(12f);
                ImGui.TextColored(MementoColors.Text, $"Action: {action}");
                ImGui.Spacing();
                ImGui.SetCursorPosX(12f);
                ImGui.TextColored(MementoColors.Dim with { W = 0.70f }, "This cannot be undone.");
                ImGui.Spacing(); ImGui.Spacing();
                ImGui.SetCursorPosX(12f);

                PushRedBtn();
                if (ImGui.Button($"Yes, confirm##yes", new Vector2(140f, 28f)))
                {
                    ExecuteConfirmed(action);
                    pendingConfirmAction = null;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine(0, 8f);
                if (ImGui.Button("Cancel##devcancel", new Vector2(100f, 28f)))
                {
                    pendingConfirmAction = null;
                    ImGui.CloseCurrentPopup();
                }
                if (!modalOpen) { pendingConfirmAction = null; }
                ImGui.EndPopup();
            }
        }

        private void ExecuteConfirmed(string action)
        {
            switch (action)
            {
                case "Clear Social":
                    plugin.CharData.SocialLog.Clear();
                    plugin.CharData.EmoteCounts.Clear();
                    plugin.CharData.AdmirerCounts.Clear();
                    plugin.SaveCharData();
                    Log("Social cleared", true);
                    break;
                case "Clear Events":
                    plugin.CharData.EventLog.Clear();
                    plugin.SaveCharData();
                    Log("Events cleared", true);
                    break;
            }
        }

        // Helper: grey out ImGui widgets in a using block
        private struct DisabledScope : IDisposable
        {
            private readonly bool _disabled;
            public DisabledScope(bool disabled)
            {
                _disabled = disabled;
                if (_disabled) ImGui.BeginDisabled();
            }
            public void Dispose() { if (_disabled) ImGui.EndDisabled(); }
        }

        // ── Full sandbox controls ─────────────────────────────────────────
        private void DrawSandbox(PetData pet, float W)
        {
            SectionTitle("Shadow Pet Preview", W);
            ImGui.Spacing();

            // Visual pet preview so it's clear the sandbox is working
            var dl = ImGui.GetWindowDrawList();
            var previewPos = ImGui.GetCursorScreenPos();
            float previewH = 70f;
            dl.AddRectFilled(previewPos, previewPos + new Vector2(W, previewH),
                MementoColors.ToU32(MementoColors.Panel2, 0.50f), 8f);
            dl.AddRect(previewPos, previewPos + new Vector2(W, previewH),
                MementoColors.ToU32(MementoColors.Green, 0.40f), 8f, ImDrawFlags.None, 1f);

            // Draw the shadow pet sprite
            float fs = pet.Stage switch { PetStage.Egg => 2.5f, PetStage.Baby => 2.2f, PetStage.Teen => 2.0f, _ => 1.8f };
            float sw = 25f * fs;
            PetRenderer.Draw(pet, new Vector2(previewPos.X + 10f, previewPos.Y + previewH / 2f - 14f * fs / 2f), fs, 0);

            // Stats next to sprite
            float tx = previewPos.X + 20f + sw;
            dl.AddText(new Vector2(tx, previewPos.Y + 6f), MementoColors.ToU32(MementoColors.Green), $"SANDBOX: {pet.Name}");
            dl.AddText(new Vector2(tx, previewPos.Y + 22f), MementoColors.ToU32(MementoColors.Dim),
                $"{PetManager.StageNames[(int)pet.Stage]} · {PetManager.SpeciesNames[(int)pet.Species]} · {pet.TotalEmotesReceived} emotes");

            dl.AddText(new Vector2(tx, previewPos.Y + 40f), MementoColors.ToU32(MementoColors.Pink), $"LV {(int)(pet.Love * 100)}%");
            dl.AddText(new Vector2(tx + 70f, previewPos.Y + 40f), MementoColors.ToU32(MementoColors.JoyPurple), $"JY {(int)(pet.Joy * 100)}%");
            dl.AddText(new Vector2(tx + 140f, previewPos.Y + 40f), MementoColors.ToU32(MementoColors.Amber), $"EN {(int)(pet.Energy * 100)}%");
            dl.AddText(new Vector2(tx + 210f, previewPos.Y + 40f), MementoColors.ToU32(MementoColors.Teal), $"SP {(int)(pet.Spirit * 100)}%");

            dl.AddText(new Vector2(tx, previewPos.Y + 54f), MementoColors.ToU32(MementoColors.Green), $"Vital {(int)(pet.Vitality * 100)}%");
            dl.AddText(new Vector2(tx + 100f, previewPos.Y + 54f), MementoColors.ToU32(MementoColors.Amber), $"Care {(int)pet.CareMeter}%");
            dl.AddText(new Vector2(tx + 200f, previewPos.Y + 54f), MementoColors.ToU32(MementoColors.Gold), $"Fav: {pet.FavoriteEmote}");

            ImGui.Dummy(new Vector2(W, previewH));

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Stat Controls", W);
            ImGui.Spacing();
            slidersDirty = false;
            SliderStat("LOVE", ref setLove, MementoColors.Pink, W);
            SliderStat("JOY", ref setJoy, MementoColors.JoyPurple, W);
            SliderStat("ENERGY", ref setEnergy, MementoColors.Amber, W);
            SliderStat("SPIRIT", ref setSpirit, MementoColors.Teal, W);
            if (slidersDirty) { pet.Love = setLove; pet.Joy = setJoy; pet.Energy = setEnergy; pet.Spirit = setSpirit; }

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Stage / Species", W);
            ImGui.Spacing();
            ImGui.TextColored(MementoColors.Dim, "Stage:"); ImGui.SameLine();
            foreach (var (label, ps) in new[] { ("Egg", PetStage.Egg), ("Baby", PetStage.Baby), ("Teen", PetStage.Teen), ("Adult", PetStage.Adult) })
            {
                bool active = pet.Stage == ps;
                ImGui.PushStyleColor(ImGuiCol.Button, active ? MementoColors.Purple5 with { W = 0.45f } : MementoColors.Panel2);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.25f });
                ImGui.PushStyleColor(ImGuiCol.Text, active ? MementoColors.Text : MementoColors.Dim);
                if (ImGui.Button($"{label}##s", new Vector2(58f, 22f))) { pet.Stage = ps; Log($"Stage → {label}"); }
                ImGui.PopStyleColor(3); ImGui.SameLine(0, 4f);
            }
            ImGui.NewLine();
            ImGui.TextColored(MementoColors.Dim, "Species:"); ImGui.SameLine();
            foreach (var (label, sp) in new[] { ("Fox", PetSpecies.Fox), ("Bunny", PetSpecies.Bunny), ("Sparrow", PetSpecies.Bird) })
            {
                bool active = pet.Species == sp;
                ImGui.PushStyleColor(ImGuiCol.Button, active ? MementoColors.Teal with { W = 0.45f } : MementoColors.Panel2);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Teal with { W = 0.25f });
                ImGui.PushStyleColor(ImGuiCol.Text, active ? MementoColors.Text : MementoColors.Dim);
                if (ImGui.Button($"{label}##sp", new Vector2(66f, 22f))) { pet.Species = sp; Log($"Species → {label}"); }
                ImGui.PopStyleColor(3); ImGui.SameLine(0, 4f);
            }
            ImGui.NewLine();
            ImGui.Spacing();
            PushGreenBtn();
            if (ImGui.Button("Reset Shadow Pet##rs", new Vector2(160f, 24f)))
            { shadowPet = null; EnsureShadowPet(); pet = shadowPet!; Log("Shadow pet reset"); }
            ImGui.PopStyleColor(3);

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Emote Simulator", W);
            ImGui.Spacing();
            ImGui.TextColored(MementoColors.Dim, "Player:"); ImGui.SameLine();
            ImGui.SetNextItemWidth(110f); ImGui.InputText("##pl", ref fakePlayer, 32);
            ImGui.SameLine(0, 8f);
            ImGui.TextColored(MementoColors.Dim, "Emote:"); ImGui.SameLine();
            ImGui.SetNextItemWidth(100f); ImGui.InputText("##em", ref fakeEmote, 32);
            ImGui.SameLine(0, 8f);
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Purple5 with { W = 0.25f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.45f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Text);
            if (ImGui.Button("Fire##f", new Vector2(55f, 24f))) SimEmote(pet, fakeEmote, fakePlayer);
            ImGui.PopStyleColor(3);
            ImGui.Spacing();
            ImGui.TextColored(MementoColors.Dim, "Quick:"); ImGui.SameLine();
            foreach (var e in QuickEmotes)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Panel2);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.20f });
                ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Dim);
                if (ImGui.Button($"{e}##q", new Vector2(0f, 22f))) { fakeEmote = e; SimEmote(pet, e, fakePlayer); }
                ImGui.PopStyleColor(3); ImGui.SameLine(0, 4f);
            }
            ImGui.NewLine();

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Care Meter", W);
            ImGui.Spacing();
            ImGui.SetNextItemWidth(180f);
            ImGui.SliderFloat("##care", ref setCare, 0f, 100f, "%.0f%%");
            ImGui.SameLine();
            PushGreenBtn();
            if (ImGui.Button("Set##c", new Vector2(65f, 24f))) { pet.CareMeter = setCare; Log($"Care → {setCare:0}%"); }
            ImGui.PopStyleColor(3); ImGui.SameLine(0, 4f);
            PushGreenBtn();
            if (ImGui.Button("Fill##c", new Vector2(65f, 24f))) { pet.CareMeter = 100f; setCare = 100f; Log("Care filled"); }
            ImGui.PopStyleColor(3); ImGui.SameLine(0, 4f);
            PushRedBtn();
            if (ImGui.Button("Empty##c", new Vector2(65f, 24f))) { pet.CareMeter = 0f; setCare = 0f; Log("Care emptied"); }
            ImGui.PopStyleColor(3);

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Lifecycle", W);
            ImGui.Spacing();
            PushRedBtn();
            if (ImGui.Button("+Neglect##lc", new Vector2(110f, 24f))) { pet.NeglectStreakDays++; Log($"Neglect → {pet.NeglectStreakDays}", true); }
            ImGui.SameLine(0, 6f);
            if (ImGui.Button("Reset Pet##lc", new Vector2(110f, 24f))) { shadowPet = null; EnsureShadowPet(); Log("Shadow reset"); }
            ImGui.PopStyleColor(3); ImGui.SameLine(0, 6f);
            PushGreenBtn();
            if (ImGui.Button("Reset Neglect##lc", new Vector2(130f, 24f))) { pet.NeglectStreakDays = 0; Log("Neglect reset"); }
            ImGui.SameLine(0, 6f);
            if (ImGui.Button("Reset Play CD##lc", new Vector2(130f, 24f))) { pet.LastPlayTime = null; Log("Play CD reset"); }
            ImGui.PopStyleColor(3);

            ImGui.Spacing(); ImGui.Spacing();
            SectionTitle("Data Reset (Real Config)", W);
            ImGui.Spacing();

            ImGui.TextColored(MementoColors.Red with { W = 0.80f }, "Dangerous Actions:");
            ImGui.SameLine();
            if (ImGui.Checkbox("##danger2", ref dangerousEnabled)) { }
            ImGui.SameLine();
            ImGui.TextColored(dangerousEnabled ? MementoColors.Red : MementoColors.Dim with { W = 0.55f },
                dangerousEnabled ? "ENABLED" : "Disabled — tick to unlock");
            ImGui.Spacing();

            using (new DisabledScope(!dangerousEnabled))
            {
                PushRedBtn();
                if (ImGui.Button("Clear Social##dr", new Vector2(130f, 24f)) && dangerousEnabled)
                    pendingConfirmAction = "Clear Social";
                ImGui.SameLine(0, 6f);
                if (ImGui.Button("Clear Events##dr", new Vector2(130f, 24f)) && dangerousEnabled)
                    pendingConfirmAction = "Clear Events";
                ImGui.SameLine(0, 6f);
                if (ImGui.Button("Clear Dev Log##dr", new Vector2(130f, 24f)) && dangerousEnabled)
                { devLog.Clear(); Log("Dev log cleared"); }
                ImGui.PopStyleColor(3);
            }

            DrawConfirmPopup(W);

            ImGui.Spacing(); ImGui.Spacing();
            DrawDevConsole(W);
        }

        private void SimEmote(PetData pet, string emote, string player)
        {
            var fx = GetEmoteEffect(emote);
            bool isFav = !string.IsNullOrEmpty(pet.FavoriteEmote) &&
                         string.Equals(emote, pet.FavoriteEmote, StringComparison.OrdinalIgnoreCase);
            float m = isFav ? 3.5f : 1f;
            float flat = isFav ? 0.12f : 0f;
            float lB = pet.Love, jB = pet.Joy, sB = pet.Spirit, eB = pet.Energy;
            pet.Love = Clamp(pet.Love + fx.LoveDelta * m + flat);
            pet.Joy = Clamp(pet.Joy + fx.JoyDelta * m + flat);
            pet.Spirit = Clamp(pet.Spirit + fx.SpiritDelta * m + flat);
            pet.Energy = Clamp(pet.Energy + fx.EnergyDelta * m + flat);
            pet.TotalEmotesReceived++;
            setLove = pet.Love; setJoy = pet.Joy; setEnergy = pet.Energy; setSpirit = pet.Spirit;
            string d = BuildDelta(pet.Love - lB, pet.Joy - jB, pet.Spirit - sB, pet.Energy - eB);
            Log($"{player} → {emote}{(isFav ? " [FAV]" : "")} | {(string.IsNullOrEmpty(d) ? "no change" : d)}");
        }

        private static string BuildDelta(float dL, float dJ, float dS, float dE)
        {
            var p = new List<string>();
            void A(float d, string s)
            {
                if (MathF.Abs(d) < 0.004f) return;
                string b = MathF.Abs(d) switch { > 0.06f => "+++", > 0.03f => "++", _ => "+" };
                p.Add(s + (d < 0 ? b.Replace("+", "-") : b));
            }
            A(dL, "LV"); A(dJ, "JY"); A(dS, "SP"); A(dE, "EN");
            return string.Join(" ", p);
        }

        private static EmoteEffect GetEmoteEffect(string emote) => emote switch
        {
            "Dote" => new() { LoveDelta = 0.08f, JoyDelta = 0.04f, SpiritDelta = 0.02f, EnergyDelta = 0.03f },
            "Embrace" => new() { LoveDelta = 0.05f, JoyDelta = 0.06f, SpiritDelta = 0.06f, EnergyDelta = 0.02f },
            "Hug" => new() { LoveDelta = 0.04f, JoyDelta = 0.04f, SpiritDelta = 0.05f },
            "Comfort" => new() { LoveDelta = 0.03f, SpiritDelta = 0.07f },
            "Cheer" => new() { JoyDelta = 0.05f, EnergyDelta = 0.05f },
            "Flower Shower" => new() { JoyDelta = 0.08f, SpiritDelta = 0.05f, EnergyDelta = 0.04f },
            "Furious" => new() { LoveDelta = -0.04f, JoyDelta = -0.08f, SpiritDelta = -0.02f },
            "Slap" => new() { LoveDelta = -0.03f, JoyDelta = -0.03f, EnergyDelta = 0.04f },
            "Poke" => new() { JoyDelta = 0.02f, EnergyDelta = 0.02f },
            "Blow Kiss" => new() { LoveDelta = 0.07f, JoyDelta = 0.03f, EnergyDelta = 0.02f },
            "Pet" => new() { LoveDelta = 0.06f, JoyDelta = 0.05f, SpiritDelta = 0.03f, EnergyDelta = 0.02f },
            _ => new() { LoveDelta = 0.02f, JoyDelta = 0.02f },
        };

        private void DrawDevConsole(float W)
        {
            SectionTitle("Dev Console", W);
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.04f, 0.01f, 0.10f, 0.92f));
            if (ImGui.BeginChild("##devlog", new Vector2(W, 140f), false, ImGuiWindowFlags.NoNav))
            {
                foreach (var line in devLog)
                {
                    Vector4 color;
                    if (line.StartsWith("[PINK]")) { color = MementoColors.Pink; ImGui.TextColored(color, line[6..]); }
                    else if (line.StartsWith("[PURP]")) { color = MementoColors.JoyPurple; ImGui.TextColored(color, line[6..]); }
                    else if (line.StartsWith("[TEAL]")) { color = MementoColors.Teal; ImGui.TextColored(color, line[6..]); }
                    else if (line.StartsWith("[AMBE]")) { color = MementoColors.Amber; ImGui.TextColored(color, line[6..]); }
                    else if (line.StartsWith("[GREN]")) { color = MementoColors.Green; ImGui.TextColored(color, line[6..]); }
                    else if (line.StartsWith("[RED_]")) { color = MementoColors.Red; ImGui.TextColored(color, line[6..]); }
                    else ImGui.TextColored(line.Contains("!") ? MementoColors.Amber : MementoColors.Teal, line);
                }
                if (!devLog.Any())
                    ImGui.TextColored(MementoColors.Dim with { W = 0.40f }, "Type /help for commands...");

                // Auto-scroll to show newest messages at bottom
                if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 20f)
                    ImGui.SetScrollHereY(1.0f);
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.Spacing();
            ImGui.SetNextItemWidth(W - 70f);
            bool entered = ImGui.InputText("##devcmd", ref commandInput, 256, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.SameLine(0, 4f);
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Purple5 with { W = 0.25f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Purple5 with { W = 0.45f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Text);
            bool clicked = ImGui.Button("Run##cmd", new Vector2(60f, 0f));
            ImGui.PopStyleColor(3);

            if ((entered || clicked) && !string.IsNullOrWhiteSpace(commandInput))
            {
                ExecuteCommand(commandInput.Trim());
                commandInput = "";
            }
        }

        private void ExecuteCommand(string raw)
        {
            Log($"$ {raw}");
            string[] parts = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();
            string args = parts.Length > 1 ? parts[1].Trim() : "";

            switch (cmd)
            {
                case "/help":
                    devLog.Add("[GREN]Commands:");
                    devLog.Add("[TEAL]  /emoteinfo {name}  — stat effects for an emote");
                    devLog.Add("[TEAL]  /petinfo           — show shadow/real pet stats");
                    devLog.Add("[TEAL]  /clear             — clear the console");
                    devLog.Add("[TEAL]  /help              — this list");
                    break;

                case "/clear":
                    devLog.Clear();
                    Log("Console cleared");
                    break;

                case "/emoteinfo":
                    CmdEmoteInfo(args);
                    break;

                case "/petinfo":
                    CmdPetInfo();
                    break;

                default:
                    Log($"Unknown command: {cmd}. Type /help", true);
                    break;
            }
        }

        private void CmdEmoteInfo(string emoteName)
        {
            if (string.IsNullOrEmpty(emoteName)) { Log("Usage: /emoteinfo {emote name}", true); return; }
            // Title-case the input
            emoteName = char.ToUpper(emoteName[0]) + emoteName[1..].ToLower();
            var fx = GetEmoteEffect(emoteName);
            bool isDefault = fx.LoveDelta == 0.02f && fx.JoyDelta == 0.02f && fx.SpiritDelta == 0f && fx.EnergyDelta == 0f;
            Log($"Emote: {emoteName}" + (isDefault ? " (generic)" : ""));
            if (fx.LoveDelta != 0f) devLog.Add($"[PINK]  LOVE   {(fx.LoveDelta > 0 ? "+" : "")}{(int)(fx.LoveDelta * 100)}%");
            if (fx.JoyDelta != 0f) devLog.Add($"[PURP]  JOY    {(fx.JoyDelta > 0 ? "+" : "")}{(int)(fx.JoyDelta * 100)}%");
            if (fx.SpiritDelta != 0f) devLog.Add($"[TEAL]  SPIRIT {(fx.SpiritDelta > 0 ? "+" : "")}{(int)(fx.SpiritDelta * 100)}%");
            if (fx.EnergyDelta != 0f) devLog.Add($"[AMBE]  ENERGY {(fx.EnergyDelta > 0 ? "+" : "")}{(int)(fx.EnergyDelta * 100)}%");
        }

        private void CmdPetInfo()
        {
            var pet = sandboxOn ? shadowPet : plugin.PetManager.Pet;
            if (pet == null || !pet.IsAlive) { Log("No active pet."); return; }
            Log($"Pet: {pet.Name} ({PetManager.SpeciesNames[(int)pet.Species]}, {PetManager.StageNames[(int)pet.Stage]})");
            devLog.Add($"[PINK]  Love:   {(int)(pet.Love * 100)}%");
            devLog.Add($"[PURP]  Joy:    {(int)(pet.Joy * 100)}%");
            devLog.Add($"[TEAL]  Spirit: {(int)(pet.Spirit * 100)}%");
            devLog.Add($"[AMBE]  Energy: {(int)(pet.Energy * 100)}%");
            devLog.Add($"[GREN]  Vital:  {(int)(pet.Vitality * 100)}%  Care: {(int)pet.CareMeter}%");
            if (!string.IsNullOrEmpty(pet.FavoriteEmote)) devLog.Add($"[PINK]  Fav:    {pet.FavoriteEmote} (3.5x boost)");
        }

        private void SliderStat(string label, ref float value, Vector4 color, float W)
        {
            ImGui.TextColored(color, label); ImGui.SameLine(80f);
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, color with { W = 0.80f });
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, color);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, MementoColors.Text with { W = 0.06f });
            ImGui.SetNextItemWidth(W - 160f);
            if (ImGui.SliderFloat($"##{label}sl", ref value, 0f, 1f, "")) slidersDirty = true;
            ImGui.PopStyleColor(3);
            ImGui.SameLine(0, 6f);
            ImGui.TextColored(color, $"{(int)(value * 100)}%");
        }

        private void SectionTitle(string title, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float lh = ImGui.GetTextLineHeight();
            var sz = ImGui.CalcTextSize(title.ToUpper());
            float tx = pos.X + (W - sz.X) / 2f;
            dl.AddText(new Vector2(tx, pos.Y), MementoColors.ToU32(MementoColors.Dim), title.ToUpper());
            float ly = pos.Y + lh / 2f; uint lc = MementoColors.ToU32(MementoColors.Dim, 0.18f);
            dl.AddLine(new Vector2(pos.X, ly), new Vector2(tx - 10f, ly), lc, 1f);
            dl.AddLine(new Vector2(tx + sz.X + 10f, ly), new Vector2(pos.X + W, ly), lc, 1f);
            ImGui.Dummy(new Vector2(W, lh + 2f));
        }

        private void PushRedBtn()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Red with { W = 0.20f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Red with { W = 0.38f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Red);
        }
        private void PushGreenBtn()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Green with { W = 0.20f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Green with { W = 0.38f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Green);
        }

        // ── Full config backup / restore ──────────────────────────────────
        private string ExportFullBackup()
        {
            var c = plugin.Config;
            var d = plugin.CharData;
            var sb = new System.Text.StringBuilder();
            sb.Append("{");

            // Settings
            sb.Append($"\"Theme\":\"{c.Theme}\",");
            sb.Append($"\"FontScale\":{c.FontScale.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append($"\"ShowEmoteChat\":{c.ShowEmoteChat.ToString().ToLower()},");

            // Unlocks
            sb.Append($"\"DevUnlocked\":{c.DevUnlocked.ToString().ToLower()},");
            sb.Append($"\"UnlockedRgbCat\":{c.UnlockedRgbCat.ToString().ToLower()},");
            sb.Append($"\"UnlockedSecretEgg\":{c.UnlockedSecretEgg.ToString().ToLower()},");

            // Redeemed codes
            sb.Append("\"RedeemedCodes\":[");
            sb.Append(string.Join(",", c.RedeemedCodes.Select(x => $"\"{Esc(x)}\"")));
            sb.Append("],");

            // Tracked emotes
            sb.Append("\"TrackedEmotes\":[");
            sb.Append(string.Join(",", c.TrackedEmotes.Select(x => $"\"{Esc(x)}\"")));
            sb.Append("],");

            // Social — emote counts
            sb.Append("\"EmoteCounts\":{");
            sb.Append(string.Join(",", d.EmoteCounts.Select(kv => $"\"{Esc(kv.Key)}\":{kv.Value}")));
            sb.Append("},");

            // Social — admirer counts (best friends)
            sb.Append("\"AdmirerCounts\":{");
            sb.Append(string.Join(",", d.AdmirerCounts.Select(kv => $"\"{Esc(kv.Key)}\":{kv.Value}")));
            sb.Append("},");

            // Observer history
            sb.Append("\"TargetCounts\":{");
            sb.Append(string.Join(",", d.TargetCounts.Select(kv => $"\"{Esc(kv.Key)}\":{kv.Value}")));
            sb.Append("},");

            // Social log (recent 50)
            sb.Append("\"SocialLog\":[");
            sb.Append(string.Join(",", d.SocialLog.Take(50).Select(x => $"\"{Esc(x)}\"")));
            sb.Append("],");

            // Event log (recent 50)
            sb.Append("\"EventLog\":[");
            sb.Append(string.Join(",", d.EventLog.Take(50).Select(x => $"\"{Esc(x)}\"")));
            sb.Append("],");

            // Pet history count (not full records for size — just count)
            sb.Append($"\"PetHistoryCount\":{d.PetHistory.Count},");

            // Current pet snapshot (basic stats — not full object)
            var pet = d.CurrentPet;
            if (pet != null && pet.IsAlive && !pet.NamingPending)
            {
                sb.Append("\"Pet\":{");
                sb.Append($"\"Name\":\"{Esc(pet.Name)}\",");
                sb.Append($"\"Species\":{(int)pet.Species},");
                sb.Append($"\"Stage\":{(int)pet.Stage},");
                sb.Append($"\"Tier\":{(int)pet.Tier},");
                sb.Append($"\"Love\":{pet.Love.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"Joy\":{pet.Joy.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"Spirit\":{pet.Spirit.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"Energy\":{pet.Energy.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"CareMeter\":{pet.CareMeter.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
                sb.Append($"\"TotalEmotes\":{pet.TotalEmotesReceived},");
                sb.Append($"\"SoulEmote\":\"{Esc(pet.BonusEmote)}\","); // Key kept as "SoulEmote" for backup compat
                sb.Append($"\"NeglectDays\":{pet.NeglectStreakDays}");
                sb.Append("},");
            }

            sb.Append("\"V\":2}"); // version marker
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private string ImportFullBackup(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "✕ Empty token.";
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                if (!json.Contains("\"V\":2")) return "✕ Not a valid Memento backup token.";

                var c = plugin.Config;
                System.Func<string, string, System.Text.RegularExpressions.Match> rx =
                    System.Text.RegularExpressions.Regex.Match;

                // Settings
                var tm = rx(json, "\"Theme\":\"([^\"]+)\"");
                if (tm.Success) c.Theme = tm.Groups[1].Value;
                var fm = rx(json, "\"FontScale\":([0-9.]+)");
                if (fm.Success)
                {
                    float fs = 1.0f;
                    if (float.TryParse(fm.Groups[1].Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out fs))
                        c.FontScale = fs;
                }
                if (json.Contains("\"ShowEmoteChat\":true")) c.ShowEmoteChat = true;
                if (json.Contains("\"ShowEmoteChat\":false")) c.ShowEmoteChat = false;

                // Unlocks
                if (json.Contains("\"DevUnlocked\":true")) c.DevUnlocked = true;
                if (json.Contains("\"UnlockedRgbCat\":true")) c.UnlockedRgbCat = true;
                if (json.Contains("\"UnlockedSecretEgg\":true")) c.UnlockedSecretEgg = true;

                // Emote counts — parse simple key:value dict
                var d = plugin.CharData;
                ParseIntDict(json, "EmoteCounts", d.EmoteCounts);
                ParseIntDict(json, "AdmirerCounts", d.AdmirerCounts);
                ParseIntDict(json, "TargetCounts", d.TargetCounts);

                // Tracked emotes
                var teMatch = rx(json, "\"TrackedEmotes\":\\[([^\\]]*)\\]");
                if (teMatch.Success)
                {
                    c.TrackedEmotes.Clear();
                    foreach (System.Text.RegularExpressions.Match m2 in
                        System.Text.RegularExpressions.Regex.Matches(teMatch.Groups[1].Value, "\"([^\"]+)\""))
                        c.TrackedEmotes.Add(m2.Groups[1].Value);
                }

                c.Save();
                plugin.SaveCharData();
                return "✓ Backup restored successfully!";
            }
            catch (Exception ex) { return $"✕ Import failed: {ex.Message}"; }
        }

        private static void ParseIntDict(string json, string key, Dictionary<string, int> target)
        {
            var blockMatch = System.Text.RegularExpressions.Regex.Match(
                json, $"\"{key}\":\\{{([^}}]*)\\}}");
            if (!blockMatch.Success) return;
            string block = blockMatch.Groups[1].Value;
            foreach (System.Text.RegularExpressions.Match m in
                System.Text.RegularExpressions.Regex.Matches(block, "\"([^\"]+)\":([0-9]+)"))
            {
                if (int.TryParse(m.Groups[2].Value, out int v))
                    target[m.Groups[1].Value] = v;
            }
        }

        private static string Esc(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"")
             .Replace("\n", "\\n").Replace("\r", "");

        private static float Clamp(float v) => Math.Max(0f, Math.Min(1f, v));
    }
}
