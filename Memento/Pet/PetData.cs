using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Memento.Pet
{
    public enum PetStage { Egg = 0, Baby = 1, Teen = 2, Adult = 3, Dead = 4 }
    public enum PetSpecies { Fox = 0, Bunny = 1, Bird = 2 }
    public enum EggTier { Bronze = 0, Silver = 1, Gold = 2 }
    public enum CareLevel { Good, Warning, Low }
    public enum HatchState { None, Wobbling, Cracking, Revealing, Naming }

    public enum TendingActivity
    {
        Play,
        Duty,
        Crafting,
        GposE,
        Quest,
        Party,
        MobKill,
        LevelUp,
        TerraPlanting,
    }

    [Serializable]
    public class StatSnapshot
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public float Love { get; set; }
        public float Joy { get; set; }
        public float Spirit { get; set; }
        public float Energy { get; set; }
    }

    /// <summary>
    /// The living pet owned by a character. Stats range 0–1, care 0–100.
    /// Serialized as JSON inside the per-character data file.
    /// </summary>
    [Serializable]
    public class PetData
    {
        // ── Identity ──
        public string Name { get; set; } = "???";
        public PetSpecies Species { get; set; } = PetSpecies.Fox;
        public EggTier Tier { get; set; } = EggTier.Gold;
        public PetStage Stage { get; set; } = PetStage.Egg;
        public DateTime HatchedAt { get; set; } = DateTime.Now;
        public DateTime? DiedAt { get; set; } = null;
        public bool IsAlive { get; set; } = true;
        public bool NamingPending { get; set; } = true;

        // ── Core stats (0.0 – 1.0) — each influenced by different emotes ──
        public float Love { get; set; } = 0.50f;
        public float Joy { get; set; } = 0.50f;
        public float Spirit { get; set; } = 0.60f;
        public float Energy { get; set; } = 0.60f;

        /// <summary>
        /// Overall Health bar = avg(Love, Joy, Spirit, Energy) + care bonus up to +25%.
        /// </summary>
        public float Vitality =>
            Math.Clamp((Love + Joy + Spirit + Energy) / 4f
                       + (CareMeter / 100f) * 0.25f, 0f, 1f);

        // ── Lifetime counters — track total interactions across the pet's life ──
        public int TotalEmotesReceived { get; set; } = 0;
        public int TotalEmotesGiven { get; set; } = 0;
        public int MostEmotesInOneDay { get; set; } = 0;
        public int PlaydateCount { get; set; } = 0;

        public Dictionary<string, int> EmoteCountsByType { get; set; } = new();
        public List<StatSnapshot> StatHistory { get; set; } = new();
        public DateTime LastSnapshotTime { get; set; } = DateTime.Now;

        // ── Milestone timestamps — null until reached, shown in Journal ──
        public DateTime? BecameBaby { get; set; } = null;
        public DateTime? BecameTeen { get; set; } = null;
        public DateTime? BecameAdult { get; set; } = null;
        public DateTime? FirstPlaydate { get; set; } = null;

        // ── Daily care system — resets at midnight, drives stat decay rate ──
        public float CareMeter { get; set; } = 0f;
        public DateTime LastCareReset { get; set; } = DateTime.Today;
        public int NeglectStreakDays { get; set; } = 0;

        // ── Daily activity caps — prevent farming care from a single source ──
        public DateTime? LastPlayTime { get; set; } = null;
        public Dictionary<string, int> TendingUsedToday { get; set; } = new();
        public int MobKillsToday { get; set; } = 0;
        public bool TerraUsedToday { get; set; } = false;

        // ── Tick bookkeeping — used by PetManager to calculate decay between frames ──
        public DateTime LastDecayCheck { get; set; } = DateTime.Now;
        public DateTime LastEmoteTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Favourite Emote — randomly chosen at hatch. Receiving this emote gives a
        /// massive 3.5x boost to all stats. Shown in Journal. Makes each pet feel unique.
        /// Serialized as "SoulEmote" for backward compat with existing save files.
        /// </summary>
        [JsonProperty("SoulEmote")]
        public string BonusEmote { get; set; } = "";

        /// <summary>Public-facing name for the bonus emote. Use this everywhere in UI code.</summary>
        [JsonIgnore]
        public string FavoriteEmote => BonusEmote;

        /// <summary>Count of times the favourite emote has been received.</summary>
        [JsonIgnore]
        public int FavoriteEmoteCount => string.IsNullOrEmpty(BonusEmote) ? 0
            : EmoteCountsByType.GetValueOrDefault(BonusEmote);

        /// <summary>The emote received most often (purely cosmetic stat).</summary>
        public string MostReceivedEmote => EmoteCountsByType.Count == 0
            ? "None yet"
            : EmoteCountsByType.OrderByDescending(e => e.Value).First().Key;

        public int MostReceivedEmoteCount => EmoteCountsByType.Count == 0
            ? 0
            : EmoteCountsByType.OrderByDescending(e => e.Value).First().Value;
    }

    [Serializable]
    public class PetRecord
    {
        public string Name { get; set; } = "";
        public PetSpecies Species { get; set; } = PetSpecies.Fox;
        public EggTier Tier { get; set; } = EggTier.Gold;
        public PetStage StageReached { get; set; } = PetStage.Egg;
        public DateTime HatchedAt { get; set; }
        public DateTime DiedAt { get; set; }
        public int TotalEmotesReceived { get; set; }
        public int MostEmotesInOneDay { get; set; }
        public int PlaydateCount { get; set; }
        public string FavoriteEmote { get; set; } = "None";
        public string CauseOfDeath { get; set; } = "Unknown";
        public float PeakLove { get; set; }
        public float PeakJoy { get; set; }
        public float PeakSpirit { get; set; }
        public float PeakEnergy { get; set; }

        public string Lifespan
        {
            get
            {
                var s = DiedAt - HatchedAt;
                if (s.TotalDays >= 1) return $"{(int)s.TotalDays}d {s.Hours}h";
                if (s.TotalHours >= 1) return $"{s.Hours}h {s.Minutes}m";
                return $"{s.Minutes}m";
            }
        }
    }

    [Serializable]
    public class EmoteEffect
    {
        public string EmoteName { get; set; } = "";
        public float LoveDelta { get; set; } = 0f;
        public float JoyDelta { get; set; } = 0f;
        public float SpiritDelta { get; set; } = 0f;
        public float EnergyDelta { get; set; } = 0f;
    }

    [Serializable]
    public class ObserverSettings
    {
        public bool ChatNotification { get; set; } = false;
        public bool PlaySound { get; set; } = false;
        public bool DrawDotOverhead { get; set; } = true;
        public bool HighlightNameplate { get; set; } = false;

        // Dot customization — defaults will be set to active theme color in code
        public float DotColorR { get; set; } = 0.55f;
        public float DotColorG { get; set; } = 0.36f;
        public float DotColorB { get; set; } = 0.96f;
        public float DotYOffset { get; set; } = 1.6f;
        public float DotRadius { get; set; } = 15f;

        // Sound customization (FFXIV SE ID)
        public int SoundEffectId { get; set; } = 36;
    }
}
