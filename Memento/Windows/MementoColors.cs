using System.Numerics;
using Dalamud.Bindings.ImGui;
using EnzirionCore.UI;
using Memento.Pet;

namespace Memento.Windows
{
    /// <summary>
    /// Colour palette for Memento's UI — now backed by EnzirionCore's ThemePalette.
    ///
    /// Call MementoColors.Update(theme) once per frame from the draw delegate.
    /// Surface colours (Bg, Panel, Text, etc.) are read from the ThemePalette.
    /// Fixed semantic colours (Pink, Green, Amber) and emote/care helpers stay here.
    ///
    /// This adapter means ALL existing tab code works without changes —
    /// tabs still read MementoColors.Text, MementoColors.Purple5, etc.
    /// </summary>
    public static class MementoColors
    {
        // ── Active palette (updated each draw frame) ──────────────────
        private static ThemePalette _palette = ThemeRegistry.PerfectlyPurple;

        /// <summary>Sync the palette to the active theme string. Call once per frame.</summary>
        public static void Update(string theme)
        {
            _palette = ThemeRegistry.GetByLegacyName(theme) ?? ThemeRegistry.PerfectlyPurple;
        }

        /// <summary>Direct access to the current palette for code that needs it.</summary>
        public static ThemePalette Palette => _palette;

        // ── Surface colours — delegated to palette ────────────────────
        public static Vector4 Bg => _palette.Background;
        public static Vector4 Panel => _palette.Panel;
        public static Vector4 Panel2 => _palette.Panel2;
        public static Vector4 ScreenBg => _palette.Background;  // close enough — tabs use this for dark inset backgrounds
        public static Vector4 Border => _palette.Border;
        public static Vector4 Text => _palette.Text;
        public static Vector4 Dim => _palette.TextMuted;

        // ── Accent — delegated to palette ─────────────────────────────
        public static Vector4 Purple5 => _palette.Accent;
        public static Vector4 Purple6 => _palette.AccentHi;

        // ── Fixed semantic colors (don't change with theme) ───────────
        public static readonly Vector4 JoyPurple = new(0.65f, 0.55f, 0.98f, 1.00f);
        public static readonly Vector4 Pink = new(1.00f, 0.30f, 0.55f, 1.00f);
        public static readonly Vector4 Pink2 = new(1.00f, 0.62f, 0.82f, 1.00f);
        public static readonly Vector4 Teal = new(0.18f, 0.83f, 0.75f, 1.00f);
        public static readonly Vector4 Green = new(0.29f, 0.68f, 0.50f, 1.00f);
        public static readonly Vector4 Amber = new(0.98f, 0.75f, 0.14f, 1.00f);
        public static readonly Vector4 Red = new(0.97f, 0.44f, 0.44f, 1.00f);
        public static readonly Vector4 Gold = new(1.00f, 0.84f, 0.00f, 1.00f);

        // ── Emote helpers ─────────────────────────────────────────────
        public static Vector4 GetEmoteColor(string emote) => emote switch
        {
            "Dote" or "Blow Kiss" => Pink,
            "Pet" => Pink2,
            "Embrace" or "Hug" => Purple5,
            "Flower Shower" => Teal,
            "Cheer" => Amber,
            "Furious" or "Slap" => Red,
            _ => Purple5,
        };

        public static (Vector4 bg, Vector4 fg) GetEmoteTagColors(string emote) => emote switch
        {
            "Dote" or "Blow Kiss" => (Pink with { W = 0.12f }, Pink),
            "Pet" => (Pink2 with { W = 0.12f }, Pink2),
            "Embrace" or "Hug" => (Purple5 with { W = 0.12f }, Purple5),
            "Flower Shower" => (Teal with { W = 0.12f }, Teal),
            "Cheer" => (Amber with { W = 0.12f }, Amber),
            "Furious" or "Slap" => (Red with { W = 0.12f }, Red),
            _ => (Purple5 with { W = 0.10f }, Purple5),
        };

        public static string GetEmoteIcon(string emote) => emote switch
        {
            "Dote" or "Pet" or "Blow Kiss" or "Hug" => "♥",
            "Embrace" => "✦",
            "Flower Shower" => "✿",
            "Cheer" => "★",
            "Furious" or "Slap" => "!",
            _ => "·",
        };

        public static Vector4 GetCareColor(CareLevel level) => level switch
        {
            CareLevel.Good => Green,
            CareLevel.Warning => Amber,
            _ => Red,
        };

        public static string GetTendingIcon(string activityKey) => activityKey switch
        {
            "Play" => "♪",
            "Duty" => "⚔",
            "Crafting" => "✦",
            "GposE" => "✿",
            _ => "·",
        };

        // ── Colour conversion — delegates to EnzirionCore.UI.ColorUtil ──
        public static uint ToU32(Vector4 v) => ColorUtil.ToU32(v);
        public static uint ToU32(Vector4 v, float alpha) => ColorUtil.ToU32(v, alpha);
    }
}
