using EnzirionCore.Auth;
using EnzirionCore.Data;
using Memento.Pet;

namespace Memento
{
    /// <summary>
    /// Shared plugin settings that apply across all characters.
    /// Per-character data (pet, social, events, observer) lives in CharacterData.
    /// Version 8+ = per-character migration complete; Version 7 = legacy (all data here).
    ///
    /// Now inherits BaseConfiguration — Initialize/Save/DevMode are handled by the base class.
    /// </summary>
    [Serializable]
    public class Configuration : BaseConfiguration
    {
        // Version is inherited from BaseConfiguration
        public bool IsFirstRun { get; set; } = true;

        // ── Legacy fields (pre-v8) — kept for migration, cleared after first load ──
        public List<string> SocialLog { get; set; } = new();
        public List<string> TrackedEmotes { get; set; } = new();
        public List<string> EventLog { get; set; } = new();

        public bool ShowEmoteChat { get; set; } = true;

        public ObserverSettings ObserverSettings { get; set; } = new();

        public Dictionary<string, int> EmoteCounts { get; set; } = new();
        public Dictionary<string, int> AdmirerCounts { get; set; } = new();
        public Dictionary<string, int> TargetCounts { get; set; } = new();
        public Dictionary<string, int> TargetToday { get; set; } = new();
        public DateTime TargetDayReset { get; set; } = DateTime.Today;

        [NonSerialized] public HashSet<string> CurrentlyTargeting = new();

        // ── Shared appearance settings (same across all characters) ──
        public string Theme { get; set; } = "Perfectly Purple";
        public float FontScale { get; set; } = 1.0f;

        // ── Legacy pet fields (pre-v8) — moved to CharacterData after migration ──
        public PetData? CurrentPet { get; set; } = null;
        public List<PetRecord> PetHistory { get; set; } = new();
        public List<EmoteEffect> EmoteEffects { get; set; } = new();

        // ── Cloud sync (Enzirion integration — now uses shared EnzirionAccount) ──
        public EnzirionAccount Enzirion { get; set; } = new();

        // Legacy cloud fields — kept for migration from old config
        public bool EnzirionEnabled { get; set; } = false;
        public string EnzirionToken { get; set; } = "";
        public string EnzirionCharacterName { get; set; } = "";
        public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
        public string LastSyncStatus { get; set; } = "";

        // ── Dev & unlocks — gated content and redeemed codes ──
        public bool DevUnlocked { get; set; } = false;
        public List<string> RedeemedCodes { get; set; } = new();
        public bool UnlockedRgbCat { get; set; } = false;
        public bool UnlockedSecretEgg { get; set; } = false;

        // ── Runtime-only state (not persisted) ──
        [NonSerialized] public bool TerraInstalled = false;

        // DevMode is now inherited from BaseConfiguration

        /// <summary>
        /// Called automatically by BaseConfiguration.Initialize().
        /// Handles first-run default emotes.
        /// </summary>
        protected override void OnInitialize()
        {
            if (IsFirstRun)
            {
                TrackedEmotes.AddRange(new[]
                    { "Dote", "Pet", "Embrace", "Blow Kiss", "Flower Shower", "Furious" });
                IsFirstRun = false;
                Save();
            }
        }

        // ── Legacy helpers — these stay in Configuration for now ──

        public void LogEvent(string icon, string description)
        {
            EventLog.Insert(0, $"{icon}|{description}|{DateTime.Now:t}");
            if (EventLog.Count > 50) EventLog.RemoveAt(50);
        }

        public void RecordTargeting(string playerName)
        {
            if (DateTime.Today > TargetDayReset.Date)
            {
                TargetToday.Clear();
                TargetDayReset = DateTime.Today;
            }
            TargetCounts[playerName] = TargetCounts.GetValueOrDefault(playerName) + 1;
            TargetToday[playerName] = TargetToday.GetValueOrDefault(playerName) + 1;
        }

        // NOTE: Save() and Initialize() are inherited from BaseConfiguration.
        // DevMode guard is handled by the base class.
    }
}
