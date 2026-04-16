using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;
using Memento.Pet;
using System;
using System.Numerics;

namespace Memento.Windows
{
    /// <summary>
    /// Floating daily care checklist. Read-only — items are checked automatically
    /// by in-game actions. The player cannot manually tick these.
    /// </summary>
    public class DailyChecklistWindow : StandardWindow
    {
        protected override ThemePalette GetCurrentPalette() =>
            ThemeRegistry.GetByLegacyName(plugin.Config.Theme) ?? ThemeRegistry.PerfectlyPurple;

        private readonly Plugin plugin;

        public DailyChecklistWindow(Plugin plugin)
            : base("Daily Care Checklist###MementoChecklist",
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.plugin = plugin;
            this.IsOpen = false;
            SizeCondition = ImGuiCond.FirstUseEver;
            Size = new Vector2(420f, 0f); // auto height
        }

        public override void Dispose() { }

        public override void Draw()
        {
            var pet = plugin.PetManager.Pet;
            float scrollW = ImGui.GetStyle().ScrollbarSize;
            float W = ImGui.GetContentRegionAvail().X - scrollW;

            // Care meter summary
            float care = pet?.CareMeter ?? 0f;
            var careColor = MementoColors.GetCareColor(plugin.PetManager.GetCareLevel());

            ImGui.TextColored(MementoColors.Dim, "DAILY CARE PROGRESS");
            ImGui.SameLine(0, 10f);
            ImGui.TextColored(careColor, $"{(int)care}%");

            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float barH = 6f;
            dl.AddRectFilled(pos, pos + new Vector2(W, barH),
                MementoColors.ToU32(MementoColors.Text, 0.06f), 3f);
            if (care > 0f)
                dl.AddRectFilled(pos, pos + new Vector2(W * Math.Clamp(care / 100f, 0f, 1f), barH),
                    MementoColors.ToU32(careColor), 3f);
            ImGui.Dummy(new Vector2(W, barH + 2f));

            ImGui.TextColored(MementoColors.Dim with { W = 0.50f },
                "These are checked automatically from your in-game actions.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (pet == null || !pet.IsAlive || pet.NamingPending)
            {
                ImGui.TextColored(MementoColors.Dim, "Hatch a pet to see the checklist.");
                return;
            }

            // ── Activities ───────────────────────────────────────────────
            DrawSectionLabel("Activities");

            bool playDone = pet.LastPlayTime.HasValue &&
                            (DateTime.Now - pet.LastPlayTime.Value).TotalHours < 4.0;
            DrawItem("♪", "Play with pet",
                playDone ? "Done!" : plugin.PetManager.CanPlayNow()
                    ? "Ready — press Play in the Pet tab"
                    : $"Ready in {plugin.PetManager.PlayCooldownString()}",
                "+15–20% care · once every 4 hours",
                playDone, MementoColors.Purple5);

            int dutyUsed = plugin.PetManager.GetTendingUsed(TendingActivity.Duty);
            int dutyCap = plugin.PetManager.GetTendingCap(TendingActivity.Duty);
            DrawItem("⚔", "Complete a duty",
                dutyUsed >= dutyCap ? $"Done! ({dutyCap}/{dutyCap})" : $"{dutyUsed}/{dutyCap} today",
                "+20% care · up to 3× per day — roulettes, raids, trials",
                dutyUsed >= dutyCap, MementoColors.Amber);

            int questUsed = plugin.PetManager.GetTendingUsed(TendingActivity.Quest);
            int questCap = plugin.PetManager.GetTendingCap(TendingActivity.Quest);
            DrawItem("!", "Finish a quest",
                questUsed >= questCap ? $"Done! ({questCap}/{questCap})" : $"{questUsed}/{questCap} today",
                "+10% care · up to 5× per day — any quest",
                questUsed >= questCap, MementoColors.Green);

            bool partyDone = plugin.PetManager.GetTendingUsed(TendingActivity.Party) >= 1;
            DrawItem("⚑", "Join a party",
                partyDone ? "Done!" : "Not yet today",
                "+5% care · once per day",
                partyDone, MementoColors.Teal);

            int gposeUsed = plugin.PetManager.GetTendingUsed(TendingActivity.GposE);
            int gposeCap = plugin.PetManager.GetTendingCap(TendingActivity.GposE);
            DrawItem("✿", "Enter GPose",
                gposeUsed >= gposeCap ? $"Done! ({gposeCap}/{gposeCap})" : $"{gposeUsed}/{gposeCap} today",
                "+10% care · up to 2× per day · enter GPose",
                gposeUsed >= gposeCap, MementoColors.Pink);

            int craftUsed = plugin.PetManager.GetTendingUsed(TendingActivity.Crafting);
            int craftCap = plugin.PetManager.GetTendingCap(TendingActivity.Crafting);
            DrawItem("✦", "Craft something",
                craftUsed >= craftCap ? $"Done! ({craftCap}/{craftCap})" : $"{craftUsed}/{craftCap} today",
                "+5% care · up to 4× per day",
                craftUsed >= craftCap, MementoColors.Teal);

            ImGui.Spacing();
            DrawSectionLabel("Combat");

            int mobKills = pet.MobKillsToday;
            int mobCap = 20; // kills to cap
            bool mobCapped = mobKills >= mobCap;
            DrawItem("✕", "Kill mobs",
                mobCapped ? $"Capped! ({mobKills} kills)" : $"{mobKills} kills today",
                "+1% per kill · capped at 20% per day",
                mobCapped, MementoColors.Red);

            // Level up — hidden at max level, including the section label
            int playerLevel = ClientState_LocalPlayerLevel();
            if (playerLevel is > 0 and < 100)
            {
                ImGui.Spacing();
                DrawSectionLabel("Progression");
                DrawItem("▲", "Level up",
                    "Happens automatically",
                    LevelUpNote(playerLevel),
                    false, MementoColors.Purple6);
            }

            // Terra — only if detected
            if (plugin.Config.TerraInstalled)
            {
                ImGui.Spacing();
                DrawSectionLabel("Terra · Cross-Plugin");
                bool terraDone = pet.TerraUsedToday;
                DrawItem("🌱", "Tend your crops",
                    terraDone ? "Done for today!" : "First terra action today",
                    "+10% care · water/harvest/fertilise/plant · once per day",
                    terraDone, MementoColors.Green);
            }

            ImGui.Spacing();
        }

        private void DrawSectionLabel(string label)
        {
            ImGui.TextColored(MementoColors.Dim with { W = 0.60f }, label.ToUpper());
            ImGui.Spacing();
        }

        private void DrawItem(string icon, string title, string status,
            string detail, bool done, Vector4 color)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float scrollW = ImGui.GetStyle().ScrollbarSize;
            float W = ImGui.GetContentRegionAvail().X - scrollW;
            float h = 44f;

            // Background
            var bg = done
                ? MementoColors.Green with { W = 0.06f }
                : MementoColors.Panel2;
            var border = done
                ? MementoColors.Green with { W = 0.30f }
                : MementoColors.Border;

            dl.AddRectFilled(pos, pos + new Vector2(W, h), MementoColors.ToU32(bg), 8f);
            dl.AddRect(pos, pos + new Vector2(W, h), MementoColors.ToU32(border), 8f,
                ImDrawFlags.None, done ? 1.5f : 0.8f);

            // Checkmark circle
            var cc = pos + new Vector2(18f, h / 2f);
            if (done)
            {
                dl.AddCircleFilled(cc, 9f, MementoColors.ToU32(MementoColors.Green));
                dl.AddText(cc - new Vector2(4f, 6f),
                    MementoColors.ToU32(new Vector4(0.05f, 0.18f, 0.08f, 1f)), "✓");
            }
            else
            {
                dl.AddCircle(cc, 9f, MementoColors.ToU32(MementoColors.Border), 0, 1.2f);
                dl.AddText(cc - new Vector2(4f, 6f),
                    MementoColors.ToU32(MementoColors.Dim with { W = 0.40f }), icon);
            }

            // Text — truncate detail to fit within card
            float tx = pos.X + 34f;
            float ty = pos.Y + (h - ImGui.GetTextLineHeight() * 2f - 2f) / 2f;
            dl.AddText(new Vector2(tx, ty),
                MementoColors.ToU32(done ? MementoColors.Dim : MementoColors.Text), title);

            var sSz = ImGui.CalcTextSize(status);
            float maxDetailW = W - 34f - sSz.X - 20f;
            string detailTrunc = detail;
            while (detailTrunc.Length > 4 && ImGui.CalcTextSize(detailTrunc).X > maxDetailW)
                detailTrunc = detailTrunc[..^1];
            if (detailTrunc.Length < detail.Length) detailTrunc = detailTrunc[..^1] + "\u2026";
            dl.AddText(new Vector2(tx, ty + ImGui.GetTextLineHeight() + 2f),
                MementoColors.ToU32(MementoColors.Dim with { W = 0.60f }), detailTrunc);

            // Status right
            dl.AddText(new Vector2(pos.X + W - sSz.X - 10f, pos.Y + (h - ImGui.GetTextLineHeight()) / 2f),
                MementoColors.ToU32(done ? MementoColors.Green : color), status);

            ImGui.Dummy(new Vector2(W, h));

            // Show full detail text on hover if it was truncated
            if (detailTrunc.Length < detail.Length && ImGui.IsItemHovered())
                ImGui.SetTooltip(detail);

            ImGui.Spacing();
        }

        private int ClientState_LocalPlayerLevel()
        {
            try { return (Plugin.ObjectTable.LocalPlayer as IPlayerCharacter)?.Level ?? 0; }
            catch { return 0; }
        }

        private string LevelUpNote(int level) => level switch
        {
            <= 30 => $"+5% care · you're level {level}, levelling fast!",
            <= 60 => $"+10% care · you're level {level}, moderate XP needed",
            _ => $"+20% care · you're level {level}, levelling takes effort!",
        };
    }
}
