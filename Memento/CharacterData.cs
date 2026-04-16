using System;
using System.Collections.Generic;
using EnzirionCore.Data;
using Memento.Pet;
using Newtonsoft.Json;

namespace Memento
{
    /// <summary>
    /// Per-character data — each FFXIV character gets their own file.
    /// Stored in the plugin's config directory as {contentId}.json.
    /// Shared settings (theme, font, unlocks) stay in Configuration.
    ///
    /// Now inherits BaseCharacterData — ContentId, CharacterName,
    /// Load&lt;T&gt;, Save, and BuildPath are inherited.
    /// </summary>
    [Serializable]
    public class CharacterData : BaseCharacterData
    {
        // ContentId and CharacterName are inherited from BaseCharacterData

        // Social / emote tracking
        public List<string> SocialLog { get; set; } = new();
        public List<string> EventLog { get; set; } = new();
        public Dictionary<string, int> EmoteCounts { get; set; } = new();
        public Dictionary<string, int> AdmirerCounts { get; set; } = new();
        public Dictionary<string, int> TargetCounts { get; set; } = new();
        public Dictionary<string, int> TargetToday { get; set; } = new();
        public DateTime TargetDayReset { get; set; } = DateTime.Today;

        // Observer settings (per-character so each alt can have different prefs)
        public ObserverSettings ObserverSettings { get; set; } = new();

        // Currently targeting — runtime only, not persisted
        [JsonIgnore] public HashSet<string> CurrentlyTargeting { get; set; } = new();

        // Pet
        public PetData? CurrentPet { get; set; } = null;
        public List<PetRecord> PetHistory { get; set; } = new();

        // ── Helpers ──────────────────────────────────────────────────────

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

        // ── File I/O is inherited from BaseCharacterData ──
        //
        // Usage:
        //   var data = BaseCharacterData.Load<CharacterData>(path, contentId);
        //   data.Save(path);
        //
        // NOTE: The old static Load(string, ulong) method is replaced by
        // the generic BaseCharacterData.Load<CharacterData>(path, contentId).
        // To preserve the old call sites, add this convenience method:

        /// <summary>
        /// Convenience wrapper — matches the old CharacterData.Load(path, contentId) signature.
        /// </summary>
        public static CharacterData Load(string path, ulong contentId) =>
            BaseCharacterData.Load<CharacterData>(path, contentId);
    }
}
