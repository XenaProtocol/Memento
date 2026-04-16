using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Memento.Pet;

namespace Memento.Windows.Tabs
{
    /// <summary>
    /// Journal tab — current pet stats card, milestone timeline,
    /// favourite emote display, and scrollable past pets archive.
    /// </summary>
    public class JournalTab : IDrawablePage
    {
        private readonly Plugin plugin;
        public string TabLabel => " Journal ";
        public JournalTab(Plugin plugin) { this.plugin = plugin; }

        public void Draw()
        {
            float pH = ImGui.GetContentRegionAvail().Y - 4f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##JournalScroll", new Vector2(0, pH), false))
            {
                float W = ImGui.GetContentRegionAvail().X; // inside child = excludes scrollbar
                var dl0 = ImGui.GetWindowDrawList();
                dl0.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
                ImGui.Spacing();
                DrawCurrentPet(W);
                ImGui.Spacing(); ImGui.Spacing();
                DrawMilestones(W);
                ImGui.Spacing(); ImGui.Spacing();
                DrawPastPets(W);
                ImGui.Dummy(new Vector2(W, 16f));
                dl0.PopClipRect();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawCurrentPet(float W)
        {
            DrawSectionTitle("Current Pet", W);
            ImGui.Spacing();

            var pet = plugin.PetManager.Pet;
            if (pet == null || !pet.IsAlive || pet.NamingPending)
            {
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize("No active pet — hatch one on the Pet tab!").X) / 2f);
                ImGui.TextColored(MementoColors.Dim, "No active pet — hatch one on the Pet tab!");
                return;
            }

            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float h = 178f;

            dl.AddRectFilled(pos, pos + new Vector2(W, h), MementoColors.ToU32(MementoColors.Panel2), 12f);
            dl.AddRect(pos, pos + new Vector2(W, h), MementoColors.ToU32(MementoColors.Purple5), 12f, ImDrawFlags.None, 1.5f);
            ImGui.Dummy(new Vector2(W, h));

            float spriteScale = 2.6f;
            float spriteW = 25f * spriteScale;
            int mood = plugin.PetManager.GetMoodLevel();
            PetRenderer.Draw(pet, new Vector2(pos.X + 14f, pos.Y + 14f), spriteScale, mood);

            float tx = pos.X + 14f + spriteW + 10f;
            string tierLabel = PetManager.TierNames[(int)pet.Tier];
            dl.AddText(new Vector2(tx, pos.Y + 10f), MementoColors.ToU32(MementoColors.Text), pet.Name);
            dl.AddText(new Vector2(tx + ImGui.CalcTextSize(pet.Name).X + 8f, pos.Y + 12f),
                MementoColors.ToU32(MementoColors.Dim),
                $"· {PetManager.SpeciesNames[(int)pet.Species]} · {PetManager.StageNames[(int)pet.Stage]} · {tierLabel} egg");
            dl.AddText(new Vector2(tx, pos.Y + 30f), MementoColors.ToU32(MementoColors.Dim),
                $"Hatched {pet.HatchedAt:MMM d, yyyy}  ·  Age: {plugin.PetManager.GetLifespanString()}  ·  {pet.TotalEmotesReceived} emotes");

            float barW = (W - tx - pos.X - 14f) / 2f - 8f;
            DrawMiniBar(dl, tx, pos.Y + 72f, barW, "LOVE", pet.Love, MementoColors.Pink);
            DrawMiniBar(dl, tx + barW + 8f, pos.Y + 72f, barW, "JOY", pet.Joy, MementoColors.Purple5);
            DrawMiniBar(dl, tx, pos.Y + 90f, barW, "ENERGY", pet.Energy, MementoColors.Amber);
            DrawMiniBar(dl, tx + barW + 8f, pos.Y + 90f, barW, "SPIRIT", pet.Spirit, MementoColors.Teal);
            DrawMiniBar(dl, tx, pos.Y + 110f, barW * 2f + 8f, "VITALITY", pet.Vitality, MementoColors.Green);

            float favY = pos.Y + 130f;
            dl.AddLine(new Vector2(pos.X + 12f, favY), new Vector2(pos.X + W - 12f, favY),
                MementoColors.ToU32(MementoColors.Text, 0.06f), 1f);
            dl.AddText(new Vector2(tx, favY + 8f), MementoColors.ToU32(MementoColors.Dim), "Favourite emote:");

            string favEmote = pet.FavoriteEmote;
            int favCount = pet.FavoriteEmoteCount;
            float favTextX = tx + ImGui.CalcTextSize("Favourite emote: ").X;

            var (tBg, tFg) = MementoColors.GetEmoteTagColors(favEmote);
            var tagSz = ImGui.CalcTextSize($"♥ {favEmote}");
            var tagTL = new Vector2(favTextX, favY + 6f);
            var tagBR = new Vector2(favTextX + tagSz.X + 12f, favY + tagSz.Y + 12f);
            dl.AddRectFilled(tagTL, tagBR, MementoColors.ToU32(tBg), 8f);
            dl.AddText(tagTL + new Vector2(6f, 3f), MementoColors.ToU32(tFg), $"♥ {favEmote}");

            string favInfo = $"{favCount}x  ·  3.5x boost  ·  Care: {(int)pet.CareMeter}%";
            dl.AddText(new Vector2(tagBR.X + 8f, favY + 8f),
                MementoColors.ToU32(MementoColors.Dim), favInfo);
        }

        private void DrawMiniBar(ImDrawListPtr dl, float x, float y, float w,
            string label, float value, Vector4 color)
        {
            float lW = 52f, pW = 32f, bW = w - lW - pW, bH = 5f;
            float bY = y + (ImGui.GetTextLineHeight() - bH) / 2f;
            dl.AddText(new Vector2(x, y - 1f), MementoColors.ToU32(MementoColors.Dim), label);
            var tTL = new Vector2(x + lW, bY); var tBR = new Vector2(x + lW + bW, bY + bH);
            dl.AddRectFilled(tTL, tBR, MementoColors.ToU32(MementoColors.Text, 0.06f), 2f);
            float fw = bW * Math.Clamp(value, 0f, 1f);
            if (fw > 0.5f) dl.AddRectFilled(tTL, new Vector2(tTL.X + fw, tBR.Y), MementoColors.ToU32(color), 2f);
            dl.AddText(new Vector2(tBR.X + 3f, y - 1f),
                MementoColors.ToU32(MementoColors.Purple5), $"{(int)(value * 100f)}%");
        }

        private void DrawMilestones(float W)
        {
            DrawSectionTitle("Milestones", W);
            ImGui.Spacing();

            var pet = plugin.PetManager.Pet;
            if (pet == null || !pet.IsAlive || pet.NamingPending)
            { ImGui.TextColored(MementoColors.Dim, "Hatch a pet to see milestones."); return; }

            string tierLabel = PetManager.TierNames[(int)pet.Tier];
            string speciesLabel = PetManager.SpeciesNames[(int)pet.Species];

            DrawMilestone(W, true, "Hatched from egg", $"From a {tierLabel} egg · {speciesLabel}", pet.HatchedAt.ToString("MMM d"));
            DrawMilestone(W, pet.BecameBaby != null, "Grew into a Baby", "5 emotes received", pet.BecameBaby?.ToString("MMM d") ?? "—");
            DrawMilestone(W, pet.BecameTeen != null, "Became a Teen", "20 emotes received", pet.BecameTeen?.ToString("MMM d") ?? "—");
            DrawMilestone(W, pet.BecameAdult != null, "Reached Adulthood", "50 emotes received", pet.BecameAdult?.ToString("MMM d") ?? "—");
            DrawMilestone(W, pet.FirstPlaydate != null, "First Playdate", $"{pet.PlaydateCount} playdates total", pet.FirstPlaydate?.ToString("MMM d") ?? "—",
                future: pet.FirstPlaydate == null);
        }

        private void DrawMilestone(float W, bool done, string title, string sub,
            string date, bool future = false)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float h = 34f;
            dl.AddLine(pos + new Vector2(10f, 0), pos + new Vector2(10f, h),
                MementoColors.ToU32(MementoColors.Border, 0.35f), 1f);
            var dotCenter = pos + new Vector2(10f, h / 2f);
            if (done)
            {
                dl.AddCircleFilled(dotCenter, 8f, MementoColors.ToU32(MementoColors.Purple5));
                dl.AddText(dotCenter - new Vector2(3f, 5f),
                    MementoColors.ToU32(new Vector4(0.1f, 0.05f, 0.2f, 1f)), "✓");
            }
            else
            {
                dl.AddCircleFilled(dotCenter, 8f, MementoColors.ToU32(MementoColors.Panel2));
                dl.AddCircle(dotCenter, 8f, MementoColors.ToU32(MementoColors.Border), 0, 1f);
            }
            float tx = pos.X + 26f;
            float baseY = pos.Y + (h - ImGui.GetTextLineHeight() * 2f - 2f) / 2f;
            dl.AddText(new Vector2(tx, baseY),
                MementoColors.ToU32(future ? MementoColors.Dim : MementoColors.Text), title);
            dl.AddText(new Vector2(tx, baseY + ImGui.GetTextLineHeight() + 2f),
                MementoColors.ToU32(MementoColors.Dim with { W = 0.65f }), sub);
            var dateSz = ImGui.CalcTextSize(date);
            dl.AddText(new Vector2(pos.X + W - dateSz.X - 4f,
                pos.Y + (h - ImGui.GetTextLineHeight()) / 2f),
                MementoColors.ToU32(done ? MementoColors.Purple5 : MementoColors.Dim with { W = 0.45f }), date);
            ImGui.Dummy(new Vector2(W, h));
        }

        private void DrawPastPets(float W)
        {
            DrawSectionTitle($"Past Pets  ({plugin.CharData.PetHistory.Count})", W);
            ImGui.Spacing();

            if (!plugin.CharData.PetHistory.Any())
            {
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize("No past pets yet!").X) / 2f);
                ImGui.TextColored(MementoColors.Dim, "No past pets yet!");
                return;
            }
            foreach (var record in plugin.CharData.PetHistory.OrderByDescending(r => r.HatchedAt))
                DrawPastPetCard(record, W);
        }

        private void DrawPastPetCard(PetRecord record, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float h = 90f;
            dl.AddRectFilled(pos, pos + new Vector2(W, h), MementoColors.ToU32(MementoColors.Panel2), 10f);
            dl.AddRect(pos, pos + new Vector2(W, h), MementoColors.ToU32(MementoColors.Border), 10f, ImDrawFlags.None, 1f);
            float tx = pos.X + 14f;
            string speciesIcon = record.Species switch { PetSpecies.Bunny => "🐰", PetSpecies.Bird => "🐦", _ => "🦊" };
            dl.AddText(new Vector2(tx, pos.Y + 10f), MementoColors.ToU32(MementoColors.Text),
                $"{speciesIcon}  {record.Name}");
            dl.AddText(new Vector2(tx + ImGui.CalcTextSize($"{speciesIcon}  {record.Name}  ").X, pos.Y + 12f),
                MementoColors.ToU32(MementoColors.Dim),
                $"· {PetManager.SpeciesNames[(int)record.Species]} · {PetManager.StageNames[(int)record.StageReached]}");
            dl.AddText(new Vector2(tx, pos.Y + 30f), MementoColors.ToU32(MementoColors.Dim),
                $"Lived: {record.Lifespan}  ·  Emotes: {record.TotalEmotesReceived}  ·  Playdates: {record.PlaydateCount}");
            dl.AddText(new Vector2(tx, pos.Y + 48f), MementoColors.ToU32(MementoColors.Dim),
                $"Hatched: {record.HatchedAt:MMM d, yyyy}  ·  Favourite: {record.FavoriteEmote}");

            var (causeColor, causeBg) = record.CauseOfDeath switch
            {
                "Neglect" => (MementoColors.Red, MementoColors.Red with { W = 0.15f }),
                "Released" => (MementoColors.Amber, MementoColors.Amber with { W = 0.15f }),
                _ => (MementoColors.Dim, MementoColors.Dim with { W = 0.10f }),
            };
            string causeLabel = record.CauseOfDeath switch
            {
                "Neglect" => "✕ Neglect",
                "Released" => "↑ Released",
                _ => $"✕ {record.CauseOfDeath}",
            };
            var cSz = ImGui.CalcTextSize(causeLabel);
            var cTL = new Vector2(tx, pos.Y + 66f);
            var cBR = new Vector2(tx + cSz.X + 12f, pos.Y + h - 8f);
            dl.AddRectFilled(cTL, cBR, MementoColors.ToU32(causeBg), 6f);
            dl.AddRect(cTL, cBR, MementoColors.ToU32(causeColor with { W = 0.35f }), 6f, ImDrawFlags.None, 0.7f);
            dl.AddText(cTL + new Vector2(6f, 3f), MementoColors.ToU32(causeColor), causeLabel);

            ImGui.Dummy(new Vector2(W, h));
            ImGui.Spacing();
        }

        private void DrawSectionTitle(string title, float W)
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
    }
}
