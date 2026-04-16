using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Memento.Pet
{
    /// <summary>
    /// Core pet lifecycle engine — handles hatching, stat changes, daily care,
    /// decay, stage progression, and death. Operates on the active character's
    /// pet via plugin.CharData.CurrentPet.
    /// </summary>
    public class PetManager
    {
        private readonly Plugin plugin;
        private static readonly Random Rng = new();

        // ── Stage thresholds — emotes received required to evolve ──
        private const int BabyThreshold = 5;
        private const int TeenThreshold = 20;
        private const int AdultThreshold = 50;

        // ── Spam protection — same player+emote combo ignored within window ──
        private const float EmoteCooldownSeconds = 30f;
        private readonly Dictionary<string, DateTime> lastEmoteByPlayer = new();

        // ── Care amounts ─────────────────────────────────────────────────
        private const float CarePlayAmount = 20f;  // was 25, now 15-20, using 20
        private const float CareDutyAmount = 20f;
        private const float CareCraftAmount = 5f;  // was 15, lowered heavily
        private const float CareGposeAmount = 10f;
        private const float CareQuestAmount = 10f;
        private const float CarePartyAmount = 5f;
        private const float CareMobAmount = 1f;  // per kill
        private const float CareMobDailyCap = 20f;  // max from mobs
        private const float CareTerraAmount = 10f;

        // Level-up care: higher levels = more gain (takes longer to level)
        // Level 1-30 = 5%, 30-60 = 10%, 60-99 = 20%, 100 = hidden
        public static float LevelUpCareAmount(int level) => level switch
        {
            <= 30 => 5f,
            <= 60 => 10f,
            <= 99 => 20f,
            _ => 0f,  // level 100: activity hidden
        };

        private static readonly Dictionary<TendingActivity, int> DailyCaps = new()
        {
            { TendingActivity.Play,    1 },
            { TendingActivity.Duty,    3 },
            { TendingActivity.Crafting,4 },
            { TendingActivity.GposE,   2 },
            { TendingActivity.Quest,   5 },
            { TendingActivity.Party,   1 },
        };

        // Play cooldown: 4 hours
        private static readonly TimeSpan PlayCooldown = TimeSpan.FromHours(4);

        // Decay rates per hour
        private const float DecayRateFull = 0.000f; // ≥50% care: no decay
        private const float DecayRateNormal = 0.025f; // 25-50%
        private const float DecayRateFast = 0.055f; // <25%
        private const int NeglectDeathDays = 3;

        public static readonly string[] StageNames = { "Egg", "Baby", "Teen", "Adult", "Gone" };
        public static readonly string[] SpeciesNames = { "Fox", "Bunny", "Sparrow" };
        public static readonly string[] TierNames = { "Bronze", "Silver", "Gold" };

        public static string ActivityLabel(TendingActivity a) => a switch
        {
            TendingActivity.Play => "Played with",
            TendingActivity.Duty => "Duty complete",
            TendingActivity.Crafting => "Crafted",
            TendingActivity.GposE => "GPose session",
            TendingActivity.Quest => "Quest complete",
            TendingActivity.Party => "Joined a party",
            TendingActivity.MobKill => "Mob killed",
            TendingActivity.LevelUp => "Levelled up",
            TendingActivity.TerraPlanting => "Terra: tended crops",
            _ => "Tended",
        };

        public PetManager(Plugin plugin) { this.plugin = plugin; }

        public PetData? Pet => plugin.CharData.CurrentPet;
        public bool HasLivePet => plugin.CharData.CurrentPet?.IsAlive == true;

        // ═════════════════════════════════════════════════════════════════
        // HATCH — creates a new pet with random species, tier, and favourite emote.
        // Egg tier is based on the player's lifetime emote total (rewarding veterans).
        // ═════════════════════════════════════════════════════════════════
        public EggTier DetermineEggTier()
        {
            int lifetime = plugin.CharData.PetHistory.Sum(r => r.TotalEmotesReceived)
                         + (plugin.CharData.CurrentPet?.TotalEmotesReceived ?? 0);
            if (lifetime >= 50) return EggTier.Gold;
            if (lifetime >= 20) return EggTier.Silver;
            return EggTier.Bronze;
        }

        public PetSpecies RollSpecies(EggTier tier)
        {
            int roll = Rng.Next(100);
            return tier switch
            {
                EggTier.Gold => roll < 50 ? PetSpecies.Fox : roll < 75 ? PetSpecies.Bunny : PetSpecies.Bird,
                EggTier.Silver => roll < 50 ? PetSpecies.Bunny : roll < 75 ? PetSpecies.Fox : PetSpecies.Bird,
                _ => roll < 50 ? PetSpecies.Bird : roll < 75 ? PetSpecies.Fox : PetSpecies.Bunny,
            };
        }

        // Pool of emotes that can be randomly assigned as the pet's Favourite Emote at hatch.
        // Receiving the favourite emote gives a massive 3.5x stat boost.
        private static readonly string[] FavEmotePool =
        {
            "Dote", "Embrace", "Hug", "Comfort", "Pet",
            "Cheer", "Blow Kiss", "Flower Shower",
        };

        public (EggTier tier, PetSpecies species) BeginHatch()
        {
            var tier = DetermineEggTier();
            var species = RollSpecies(tier);
            string favEmote = FavEmotePool[Rng.Next(FavEmotePool.Length)];
            plugin.CharData.CurrentPet = new PetData
            {
                Name = "", Species = species, Tier = tier,
                Stage = PetStage.Egg, IsAlive = true, NamingPending = true,
                HatchedAt = DateTime.Now, LastEmoteTime = DateTime.Now,
                LastDecayCheck = DateTime.Now, LastCareReset = DateTime.Today,
                Love = 0.40f, Joy = 0.40f, Spirit = 0.60f, Energy = 0.60f,
                BonusEmote = favEmote,
            };
            plugin.SaveCharData();
            return (tier, species);
        }

        public void FinaliseName(string name)
        {
            if (plugin.CharData.CurrentPet == null) return;
            plugin.CharData.CurrentPet.Name = string.IsNullOrWhiteSpace(name)
                ? SpeciesNames[(int)plugin.CharData.CurrentPet.Species]
                : name.Trim();
            plugin.CharData.CurrentPet.NamingPending = false;
            plugin.CharData.LogEvent("*", $"{plugin.CharData.CurrentPet.Name} hatched from a {TierNames[(int)plugin.CharData.CurrentPet.Tier]} egg!");
            plugin.SaveCharData();
        }

        // ═════════════════════════════════════════════════════════════════
        // TICK — called every frame from Plugin.OnUpdate.
        // Handles midnight care reset, stat decay based on care level,
        // and death check (3 neglect days + near-zero vitality = death).
        // ═════════════════════════════════════════════════════════════════
        public void Tick()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive || pet.NamingPending) return;

            if (DateTime.Today > pet.LastCareReset.Date) MidnightReset();

            double hours = (DateTime.Now - pet.LastDecayCheck).TotalHours;
            if (hours < 0.25) return;

            float rate = pet.CareMeter >= 50f ? DecayRateFull
                       : pet.CareMeter >= 25f ? DecayRateNormal
                       : DecayRateFast;

            if (rate > 0f)
            {
                float d = (float)(hours * rate);
                pet.Love = Max0(pet.Love - d * 1.0f);
                pet.Joy = Max0(pet.Joy - d * 0.9f);
                pet.Spirit = Max0(pet.Spirit - d * 0.5f); // Spirit decays slowest
                pet.Energy = Max0(pet.Energy - d * 0.7f);
            }

            pet.LastDecayCheck = DateTime.Now;

            // Death: Vitality near zero for 3 neglect days
            if (pet.NeglectStreakDays >= NeglectDeathDays && pet.Vitality <= 0.02f)
            {
                KillPet("Neglect");
                return;
            }
            plugin.SaveCharData();
        }

        // ═════════════════════════════════════════════════════════════════
        // EMOTE RECEIVED — the main stat-change entry point.
        // Applies emote-specific effects to Love/Joy/Spirit/Energy,
        // multiplied by 3.5x if it matches the pet's Favourite Emote.
        // Returns a compact delta string (e.g. "♥♥♥ ★★ ✦") for the event log.
        // ═════════════════════════════════════════════════════════════════
        public (bool fed, string deltaStr) OnEmoteReceived(string emoteName, string playerName)
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive || pet.NamingPending) return (false, "");

            string key = $"{playerName}|{emoteName}";
            if (lastEmoteByPlayer.TryGetValue(key, out var last)
                && (DateTime.Now - last).TotalSeconds < EmoteCooldownSeconds)
                return (false, "");

            lastEmoteByPlayer[key] = DateTime.Now;

            var fx = GetEmoteEffect(emoteName);

            // Favourite Emote bonus — randomly chosen at hatch, gives 3.5x boost
            bool isFav = !string.IsNullOrEmpty(pet.FavoriteEmote) &&
                         string.Equals(emoteName, pet.FavoriteEmote, StringComparison.OrdinalIgnoreCase);
            float soulMult = isFav ? 3.5f : 1f;

            float lB = pet.Love, jB = pet.Joy, sB = pet.Spirit, eB = pet.Energy;
            pet.Love = Clamp(pet.Love + fx.LoveDelta * soulMult + (isFav ? 0.12f : 0f));
            pet.Joy = Clamp(pet.Joy + fx.JoyDelta * soulMult + (isFav ? 0.12f : 0f));
            pet.Spirit = Clamp(pet.Spirit + fx.SpiritDelta * soulMult + (isFav ? 0.12f : 0f));
            pet.Energy = Clamp(pet.Energy + fx.EnergyDelta * soulMult + (isFav ? 0.12f : 0f));

            pet.TotalEmotesReceived++;
            pet.LastEmoteTime = DateTime.Now;
            pet.EmoteCountsByType[emoteName] = pet.EmoteCountsByType.GetValueOrDefault(emoteName) + 1;

            string delta = BuildDeltaStr(pet.Love - lB, pet.Joy - jB, pet.Spirit - sB, pet.Energy - eB);
            if (isFav) delta = "♥ FAV  " + delta;
            CheckStageProgression(pet);
            plugin.SaveCharData();
            return (true, delta);
        }

        /// <summary>
        /// Build a compact delta string using repeated stat-specific symbols.
        /// ♥ = Love (pink), ★ = Joy (purple), ✦ = Spirit (teal), ⚡ = Energy (amber)
        /// Repeated 1-3x based on magnitude. Negative uses ✕ instead.
        /// </summary>
        private static string BuildDeltaStr(float dL, float dJ, float dS, float dE)
        {
            var sb = new StringBuilder();
            void Add(float d, string posSym, string negSym)
            {
                if (Math.Abs(d) < 0.005f) return;
                int mag = Math.Abs(d) switch { > 0.06f => 3, > 0.03f => 2, _ => 1 };
                string sym = d > 0 ? posSym : negSym;
                if (sb.Length > 0) sb.Append(' ');
                for (int i = 0; i < mag; i++) sb.Append(sym);
            }
            Add(dL, "♥", "✕");  // Love: pink hearts / red X
            Add(dJ, "★", "✕");  // Joy: purple stars
            Add(dS, "✦", "✕");  // Spirit: teal sparkles
            Add(dE, "⚡", "✕"); // Energy: amber lightning
            return sb.ToString();
        }

        public void OnEmoteGiven()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive) return;
            pet.Energy = Clamp(pet.Energy + 0.02f);
            pet.Joy = Clamp(pet.Joy + 0.01f);
            pet.TotalEmotesGiven++;
            plugin.SaveCharData();
        }

        // ═════════════════════════════════════════════════════════════════
        // TENDING — daily care activities that fill the care meter (0–100%).
        // Each activity has daily caps to prevent farming. Higher care = less
        // stat decay. Called from Plugin when game events are detected.
        // ═════════════════════════════════════════════════════════════════
        public TendingResult Tend(TendingActivity activity, int levelIfLevelUp = 0)
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive || pet.NamingPending)
                return new TendingResult(false, "No pet to tend.");

            // Mob kills: per-kill, separate cap logic
            if (activity == TendingActivity.MobKill)
            {
                float mobTotal = pet.MobKillsToday * CareMobAmount;
                if (mobTotal >= CareMobDailyCap)
                    return new TendingResult(false, "Mob care cap reached for today");
                pet.MobKillsToday++;
                pet.CareMeter = Math.Min(100f, pet.CareMeter + CareMobAmount);
                plugin.SaveCharData();
                return new TendingResult(true, $"Mob kill +{CareMobAmount}% care");
            }

            // Level-up: no daily cap, but hidden at 100
            if (activity == TendingActivity.LevelUp)
            {
                float amount = LevelUpCareAmount(levelIfLevelUp);
                if (amount <= 0f) return new TendingResult(false, "Max level — no care gain");
                float before = pet.CareMeter;
                pet.CareMeter = Math.Min(100f, pet.CareMeter + amount);
                plugin.SaveCharData();
                return new TendingResult(true,
                    $"Level up (Lv{levelIfLevelUp}) +{(int)(pet.CareMeter - before)}% care");
            }

            // Terra: boolean per day, first action only
            if (activity == TendingActivity.TerraPlanting)
            {
                if (pet.TerraUsedToday)
                    return new TendingResult(false, "Terra care already gained today");
                pet.TerraUsedToday = true;
                pet.CareMeter = Math.Min(100f, pet.CareMeter + CareTerraAmount);
                plugin.SaveCharData();
                return new TendingResult(true, $"Terra tending +{CareTerraAmount}% care");
            }

            // Play: 4-hour cooldown
            if (activity == TendingActivity.Play)
            {
                if (pet.LastPlayTime.HasValue &&
                    (DateTime.Now - pet.LastPlayTime.Value) < PlayCooldown)
                {
                    var rem = PlayCooldown - (DateTime.Now - pet.LastPlayTime.Value);
                    return new TendingResult(false,
                        $"Can play again in {(int)rem.TotalHours}h {rem.Minutes}m");
                }
                pet.LastPlayTime = DateTime.Now;
            }

            // Daily cap check
            string actKey = activity.ToString();
            pet.TendingUsedToday.TryGetValue(actKey, out int used);
            int cap = DailyCaps.GetValueOrDefault(activity, 1);
            if (used >= cap)
                return new TendingResult(false,
                    $"{ActivityLabel(activity)} already used {cap}x today");

            float careAmount = activity switch
            {
                TendingActivity.Play => CarePlayAmount,
                TendingActivity.Duty => CareDutyAmount,
                TendingActivity.Crafting => CareCraftAmount,
                TendingActivity.GposE => CareGposeAmount,
                TendingActivity.Quest => CareQuestAmount,
                TendingActivity.Party => CarePartyAmount,
                _ => 5f,
            };

            float b = pet.CareMeter;
            pet.CareMeter = Math.Min(100f, pet.CareMeter + careAmount);
            pet.TendingUsedToday[actKey] = used + 1;
            plugin.SaveCharData();

            return new TendingResult(true,
                $"{ActivityLabel(activity)} +{(int)(pet.CareMeter - b)}% care ({(int)pet.CareMeter}% total)");
        }

        // ═════════════════════════════════════════════════════════════════
        // KILL — archives the pet to PetHistory and clears CurrentPet.
        // Triggered by neglect (3+ days) or manual release from Dev tab.
        // ═════════════════════════════════════════════════════════════════
        public void KillPet(string cause = "Unknown")
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null) return;
            pet.IsAlive = false;
            pet.DiedAt = DateTime.Now;
            pet.Stage = PetStage.Dead;

            plugin.CharData.PetHistory.Add(new PetRecord
            {
                Name = pet.Name,
                Species = pet.Species,
                Tier = pet.Tier,
                StageReached = pet.Stage,
                HatchedAt = pet.HatchedAt,
                DiedAt = pet.DiedAt ?? DateTime.Now,
                TotalEmotesReceived = pet.TotalEmotesReceived,
                PlaydateCount = pet.PlaydateCount,
                FavoriteEmote = pet.FavoriteEmote,
                CauseOfDeath = cause,
                PeakLove = pet.Love,
                PeakJoy = pet.Joy,
                PeakSpirit = pet.Spirit,
                PeakEnergy = pet.Energy,
            });
            plugin.CharData.CurrentPet = null;
            plugin.SaveCharData();
        }

        // ═════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Called on first tick after midnight — zeros care meter,
        /// resets daily caps, and increments neglect streak if care was low.</summary>
        private void MidnightReset()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null) return;
            pet.NeglectStreakDays = pet.CareMeter < 25f ? pet.NeglectStreakDays + 1 : 0;
            pet.CareMeter = 0f;
            pet.LastCareReset = DateTime.Today;
            pet.MobKillsToday = 0;
            pet.TerraUsedToday = false;
            pet.TendingUsedToday.Clear();
            plugin.SaveCharData();
        }

        private void CheckStageProgression(PetData pet)
        {
            int r = pet.TotalEmotesReceived;
            var newStage = r >= AdultThreshold ? PetStage.Adult
                         : r >= TeenThreshold ? PetStage.Teen
                         : r >= BabyThreshold ? PetStage.Baby
                         : PetStage.Egg;
            if (newStage == pet.Stage) return;
            pet.Stage = newStage;
            switch (newStage)
            {
                case PetStage.Baby: pet.BecameBaby = DateTime.Now; break;
                case PetStage.Teen: pet.BecameTeen = DateTime.Now; break;
                case PetStage.Adult: pet.BecameAdult = DateTime.Now; break;
            }
            plugin.CharData.LogEvent("✦", $"{pet.Name} grew into a {StageNames[(int)newStage]}!");
        }

        // Mood 0=happy 1=neutral 2=sad — driven by Vitality
        public int GetMoodLevel()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive) return 2;
            float v = pet.Vitality;
            if (v >= 0.60f) return 0;
            if (v >= 0.30f) return 1;
            return 2;
        }

        public string GetMoodString()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive) return "Gone...";
            float v = pet.Vitality;
            if (v >= 0.80f) return "Thriving — loves you!";
            if (v >= 0.60f) return "Happy and content";
            if (v >= 0.40f) return "Doing okay";
            if (v >= 0.20f) return "Feeling lonely...";
            return "Please love me...";
        }

        public string GetMoodIcon()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null || !pet.IsAlive) return "✕";
            float v = pet.Vitality;
            if (v >= 0.80f) return "✦";
            if (v >= 0.60f) return "♥";
            if (v >= 0.40f) return "·";
            if (v >= 0.20f) return "…";
            return "!";
        }

        public string GetLifespanString()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null) return "0m";
            var end = pet.IsAlive ? DateTime.Now : (pet.DiedAt ?? DateTime.Now);
            var span = end - pet.HatchedAt;
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d {span.Hours}h";
            if (span.TotalHours >= 1) return $"{span.Hours}h {span.Minutes}m";
            return $"{span.Minutes}m";
        }

        public int EmotesUntilNextStage()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null) return 0;
            int r = pet.TotalEmotesReceived;
            if (r < BabyThreshold) return BabyThreshold - r;
            if (r < TeenThreshold) return TeenThreshold - r;
            if (r < AdultThreshold) return AdultThreshold - r;
            return 0;
        }

        public bool CanPlayNow()
        {
            var pet = plugin.CharData.CurrentPet;
            return pet != null && !pet.NamingPending &&
                   (!pet.LastPlayTime.HasValue ||
                    (DateTime.Now - pet.LastPlayTime.Value) >= PlayCooldown);
        }

        public string PlayCooldownString()
        {
            if (CanPlayNow()) return "Ready!";
            var rem = PlayCooldown - (DateTime.Now - Pet!.LastPlayTime!.Value);
            return $"{(int)rem.TotalHours}h {rem.Minutes}m";
        }

        public string CareStatusString()
        {
            float c = plugin.CharData.CurrentPet?.CareMeter ?? 0f;
            if (c >= 75f) return "Well cared for";
            if (c >= 50f) return "Cared for";
            if (c >= 25f) return "Needs attention";
            return "Neglected today";
        }

        public CareLevel GetCareLevel()
        {
            float c = plugin.CharData.CurrentPet?.CareMeter ?? 0f;
            if (c >= 50f) return CareLevel.Good;
            if (c >= 25f) return CareLevel.Warning;
            return CareLevel.Low;
        }

        // How many mob kills remain today before cap
        public int MobKillsRemaining()
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null) return 0;
            int cap = (int)(CareMobDailyCap / CareMobAmount);
            return Math.Max(0, cap - pet.MobKillsToday);
        }

        // Checklist completion state helpers for DailyChecklistWindow
        public int GetTendingUsed(TendingActivity a)
        {
            var pet = plugin.CharData.CurrentPet;
            if (pet == null) return 0;
            return pet.TendingUsedToday.GetValueOrDefault(a.ToString(), 0);
        }

        public int GetTendingCap(TendingActivity a) => DailyCaps.GetValueOrDefault(a, 1);

        private EmoteEffect GetEmoteEffect(string emoteName)
        {
            var custom = plugin.Config.EmoteEffects.FirstOrDefault(e => e.EmoteName == emoteName);
            if (custom != null) return custom;
            return emoteName switch
            {
                "Dote" => new() { LoveDelta = 0.08f, JoyDelta = 0.04f, SpiritDelta = 0.02f, EnergyDelta = 0.03f },
                "Embrace" => new() { LoveDelta = 0.05f, JoyDelta = 0.06f, SpiritDelta = 0.06f, EnergyDelta = 0.02f },
                "Pet" => new() { LoveDelta = 0.06f, JoyDelta = 0.05f, SpiritDelta = 0.03f, EnergyDelta = 0.02f },
                "Blow Kiss" => new() { LoveDelta = 0.07f, JoyDelta = 0.03f, EnergyDelta = 0.02f },
                "Flower Shower" => new() { JoyDelta = 0.08f, SpiritDelta = 0.05f, EnergyDelta = 0.04f },
                "Hug" => new() { LoveDelta = 0.04f, JoyDelta = 0.04f, SpiritDelta = 0.05f },
                "Cheer" => new() { JoyDelta = 0.05f, EnergyDelta = 0.05f },
                "Comfort" => new() { LoveDelta = 0.03f, SpiritDelta = 0.07f },
                "Furious" => new() { LoveDelta = -0.04f, JoyDelta = -0.08f, SpiritDelta = -0.02f },
                "Slap" => new() { LoveDelta = -0.03f, JoyDelta = -0.03f, EnergyDelta = 0.04f },
                "Poke" => new() { JoyDelta = 0.02f, EnergyDelta = 0.02f },
                _ => new() { LoveDelta = 0.02f, JoyDelta = 0.02f },
            };
        }

        private static float Clamp(float v) => Math.Max(0f, Math.Min(1f, v));
        private static float Max0(float v) => Math.Max(0f, v);
    }

    public record TendingResult(bool Success, string Message);
}
