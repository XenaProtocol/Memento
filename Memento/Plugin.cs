using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Memento.Pet;
using Memento.Windows;

namespace Memento
{
    /// <summary>
    /// Main plugin entry point. Orchestrates per-character data, event hooks,
    /// pet lifecycle, emote/target tracking, and UI window management.
    /// All game event detection (emotes, duties, crafting, etc.) lives here
    /// and delegates stat changes to PetManager.
    ///
    /// REFACTORED: WindowCustomization removed — theme push/pop is now
    /// handled per-window by StandardWindow.PreDraw/PostDraw.
    /// </summary>
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Memento";

        public WindowSystem WindowSystem = new("Memento");

        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] internal static ICondition Condition { get; private set; } = null!;
        [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
        [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
        [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
        [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;

        public Configuration Config { get; private set; }
        public CharacterData CharData { get; private set; }
        public PetManager PetManager { get; private set; }
        private ulong currentContentId = 0;

        private MainWindow mainWindow;
        public ConfigWindow configWindow;
        public PetPopoutWindow petPopout;
        public DailyChecklistWindow checklistWindow;

        private readonly Action drawAction;

        // ── Emote & target tracking state (runtime only, not persisted) ──
        public HashSet<ulong> currentlyTargetingMe = new();
        private Dictionary<ulong, ushort> lastEmoteIds = new();
        private ushort lastLocalEmoteId = 0;

        // ── Tending detection — edge-triggered from game conditions ──
        private bool wasInGpose = false;
        private bool wasCrafting = false;
        private bool wasInParty = false;
        private int lastLevel = 0;
        private Dictionary<ulong, uint> lastMobHp = new();
        private DateTime lastTerraCheck = DateTime.MinValue;

        public Plugin()
        {
            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);  // ← BaseConfiguration.Initialize binds for Save()

            // Migrate: if old config has per-character data, move it to a character file
            MigrateGlobalToCharData();

            // Load character data for whoever is logged in (or empty placeholder)
            var contentId = PlayerState.ContentId;
            CharData = CharacterData.Load(CharDataPath(contentId), contentId);
            currentContentId = contentId;

            // Initialize dot color to match theme if not already customized
            InitializeDotColorFromTheme();

            // Detect if Terra plugin is installed
            try
            {
                Config.TerraInstalled = PluginInterface.InstalledPlugins
                    .Any(p => p.InternalName.Equals("Terra", StringComparison.OrdinalIgnoreCase) && p.IsLoaded);
            }
            catch { Config.TerraInstalled = false; }
            PetManager = new PetManager(this);

            mainWindow = new MainWindow(this);
            configWindow = new ConfigWindow(this);
            petPopout = new PetPopoutWindow(this);
            checklistWindow = new DailyChecklistWindow(this);

            WindowSystem.AddWindow(mainWindow);
            WindowSystem.AddWindow(configWindow);
            WindowSystem.AddWindow(petPopout);
            WindowSystem.AddWindow(checklistWindow);

            // ── REFACTORED draw delegate ──
            // WindowCustomization is GONE — each window inheriting StandardWindow
            // handles its own PreDraw/PostDraw theme push. We just need to sync
            // MementoColors for draw-list rendering in tabs.
            drawAction = () =>
            {
                MementoColors.Update(Config.Theme);  // sync palette for tab draw-list code
                WindowSystem.Draw();                   // each window's PreDraw/PostDraw handles theme
                DrawTargetDots();                      // render dots above players targeting us
            };

            CommandManager.AddHandler("/memento",
                new CommandInfo((c, a) => mainWindow.IsOpen = !mainWindow.IsOpen)
                { HelpMessage = "Opens Memento" });
            CommandManager.AddHandler("/mementopet",
                new CommandInfo((c, a) => petPopout.IsOpen = !petPopout.IsOpen)
                { HelpMessage = "Toggles the Memento pet popout" });

            PluginInterface.UiBuilder.Draw += drawAction;
            PluginInterface.UiBuilder.OpenMainUi += () => mainWindow.IsOpen = true;
            PluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;

            DutyState.DutyCompleted += OnDutyCompleted;
            Framework.Update += OnUpdate;
            ClientState.TerritoryChanged += OnZoneChange;
            ChatGui.ChatMessage += OnChatMessage;
        }

        /// Clears ghost targeting state when player changes zone
        private void OnZoneChange(ushort territoryId)
        {
            currentlyTargetingMe.Clear();
            CharData.CurrentlyTargeting.Clear();
            lastMobHp.Clear();
        }

        /// Quest detection — hook chat log for system messages containing "quest" completion
        private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type != XivChatType.SystemMessage && (int)type != 2105) return;
            string msg = message.TextValue;
            if (msg.EndsWith("complete!", StringComparison.OrdinalIgnoreCase) ||
                msg.EndsWith("complete.", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("You've completed", StringComparison.OrdinalIgnoreCase))
            {
                var r = PetManager.Tend(TendingActivity.Quest);
                if (r.Success) CharData.LogEvent("!", r.Message);
            }
        }

        public void TryPlayWithPet()
        {
            var result = PetManager.Tend(TendingActivity.Play);
            if (result.Success)
            {
                CharData.LogEvent("♪", result.Message);
                if (Config.ShowEmoteChat && PetManager.Pet != null)
                    SendChat($"♥ You played with {PetManager.Pet.Name}! {result.Message}");
            }
        }

        public void OpenChecklist() => checklistWindow.IsOpen = true;

        /// <summary>Sync dot color to active theme if using defaults.</summary>
        private void InitializeDotColorFromTheme()
        {
            bool isDefaultPurple = CharData.ObserverSettings.DotColorR == 0.55f &&
                                   CharData.ObserverSettings.DotColorG == 0.36f &&
                                   CharData.ObserverSettings.DotColorB == 0.96f;

            if (isDefaultPurple)
            {
                var themeColor = GetThemeColor();
                CharData.ObserverSettings.DotColorR = themeColor.X;
                CharData.ObserverSettings.DotColorG = themeColor.Y;
                CharData.ObserverSettings.DotColorB = themeColor.Z;
                SaveCharData();
            }
        }

        // ════════════════════════════════════════════════════════════════
        // FRAMEWORK UPDATE
        // ════════════════════════════════════════════════════════════════
        private void OnUpdate(IFramework framework)
        {
            CheckCharacterSwitch();
            PetManager.Tick();
            DetectTending();
            TrackMobKills();

            // Periodic Terra re-check (plugins can load/unload at runtime)
            if ((DateTime.Now - lastTerraCheck).TotalSeconds > 60)
            {
                try
                {
                    Config.TerraInstalled = PluginInterface.InstalledPlugins
                    .Any(p => p.InternalName.Equals("Terra", StringComparison.OrdinalIgnoreCase) && p.IsLoaded);
                }
                catch { }
                lastTerraCheck = DateTime.Now;
            }

            if (ObjectTable.LocalPlayer is not IPlayerCharacter local) return;
            if (local.Address == IntPtr.Zero) return;

            var myId = local.GameObjectId;
            DetectLocalEmote(local);
            DetectLevelUp(local);

            foreach (var obj in ObjectTable)
            {
                if (obj is IPlayerCharacter pc
                    && pc.Address != IntPtr.Zero
                    && pc.GameObjectId != myId)
                {
                    bool targeting = TrackTargeting(myId, pc);
                    TrackEmote(pc, targeting);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        // TENDING DETECTION
        // ════════════════════════════════════════════════════════════════
        private void DetectTending()
        {
            bool inGpose = Condition[ConditionFlag.WatchingCutscene];
            if (inGpose && !wasInGpose)
            {
                var r = PetManager.Tend(TendingActivity.GposE);
                if (r.Success) CharData.LogEvent("✿", r.Message);
            }
            wasInGpose = inGpose;

            bool crafting = Condition[ConditionFlag.Crafting];
            if (!crafting && wasCrafting)
            {
                var r = PetManager.Tend(TendingActivity.Crafting);
                if (r.Success) CharData.LogEvent("✦", r.Message);
            }
            wasCrafting = crafting;

            bool inParty = PartyList.Length > 1;
            if (inParty && !wasInParty)
            {
                var r = PetManager.Tend(TendingActivity.Party);
                if (r.Success) CharData.LogEvent("⚑", r.Message);
            }
            wasInParty = inParty;
        }

        private void OnDutyCompleted(object? sender, ushort dutyId)
        {
            var r = PetManager.Tend(TendingActivity.Duty);
            if (r.Success)
            {
                CharData.LogEvent("⚔", r.Message);
                if (Config.ShowEmoteChat && PetManager.Pet != null)
                    SendChat($"♥ Duty complete! {PetManager.Pet.Name} cheers! {r.Message}");
            }
        }

        private void DetectLevelUp(IPlayerCharacter local)
        {
            int level = local.Level;
            if (lastLevel > 0 && level > lastLevel)
            {
                var r = PetManager.Tend(TendingActivity.LevelUp, level);
                if (r.Success) CharData.LogEvent("▲", r.Message);
            }
            lastLevel = level;
        }

        // ════════════════════════════════════════════════════════════════
        // MOB KILL DETECTION
        // ════════════════════════════════════════════════════════════════
        private void TrackMobKills()
        {
            foreach (var obj in ObjectTable)
            {
                if (obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc) continue;

                ulong id = obj.GameObjectId;
                if (obj is not Dalamud.Game.ClientState.Objects.Types.ICharacter ch) continue;
                uint hp = ch.CurrentHp;

                if (lastMobHp.TryGetValue(id, out uint prev) && prev > 0 && hp == 0)
                {
                    var r = PetManager.Tend(TendingActivity.MobKill);
                    if (r.Success) CharData.LogEvent("✕", "Mob slain +1% care");
                }

                if (hp == 0) lastMobHp.Remove(id);
                else lastMobHp[id] = hp;
            }
            if (lastMobHp.Count > 200)
            {
                var keys = lastMobHp.Keys.ToList();
                foreach (var k in keys.Take(50)) lastMobHp.Remove(k);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // LOCAL EMOTE DETECTION
        // ════════════════════════════════════════════════════════════════
        private unsafe void DetectLocalEmote(IPlayerCharacter local)
        {
            try
            {
                var ch = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)local.Address;
                if (ch == null) return;
                ushort id = ch->EmoteController.EmoteId;
                if (id != 0 && id != 243 && id != lastLocalEmoteId)
                    PetManager.OnEmoteGiven();
                lastLocalEmoteId = id;
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════
        // EMOTE TRACKING
        // ════════════════════════════════════════════════════════════════
        private void TrackEmote(IPlayerCharacter pc, bool targeting)
        {
            unsafe
            {
                var ch = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)pc.Address;
                if (ch == null) return;
                try
                {
                    ushort id = ch->EmoteController.EmoteId;
                    lastEmoteIds.TryGetValue(pc.GameObjectId, out var lastId);

                    if (id != 0 && id != 243 && id != lastId && targeting)
                    {
                        var row = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>()?.GetRow(id);
                        if (row.HasValue)
                        {
                            string name = row.Value.Name.ToString();
                            bool valid = !string.IsNullOrWhiteSpace(name)
                                           && name != "=" && name.Length > 1;
                            if (valid)
                            {
                                string pName = pc.Name.ToString();
                                string firstName = pName.Contains(' ')
                                    ? pName.Split(' ')[0] : pName;

                                var (fed, deltaStr) = PetManager.OnEmoteReceived(name, pName);

                                string evDesc = string.IsNullOrEmpty(deltaStr)
                                    ? $"{firstName} → {name}"
                                    : $"{firstName} → {name}  {deltaStr}";
                                CharData.LogEvent("♥", evDesc);

                                if (Config.TrackedEmotes.Contains(name))
                                {
                                    string entry = $"[{DateTime.Now:t}] {pc.Name} used {name} on you!";
                                    CharData.SocialLog.Insert(0, entry);
                                    if (CharData.SocialLog.Count > 100) CharData.SocialLog.RemoveAt(100);
                                    CharData.EmoteCounts[name] = CharData.EmoteCounts.GetValueOrDefault(name) + 1;
                                    CharData.AdmirerCounts[pName] = CharData.AdmirerCounts.GetValueOrDefault(pName) + 1;
                                    if (Config.ShowEmoteChat && fed)
                                        SendChat($"♥ {pc.Name} used {name} on you!");
                                }
                                SaveCharData();
                            }
                        }
                    }
                    lastEmoteIds[pc.GameObjectId] = id;
                }
                catch { }
            }
        }

        // ════════════════════════════════════════════════════════════════
        // TARGET TRACKING
        // ════════════════════════════════════════════════════════════════
        private bool TrackTargeting(ulong myId, IPlayerCharacter pc)
        {
            bool targeting = pc.TargetObjectId == myId;
            string pName = pc.Name.ToString();
            string firstName = pName.Contains(' ') ? pName.Split(' ')[0] : pName;

            if (targeting && !currentlyTargetingMe.Contains(pc.GameObjectId))
            {
                currentlyTargetingMe.Add(pc.GameObjectId);
                CharData.RecordTargeting(pName);
                CharData.CurrentlyTargeting.Add(pName);
                CharData.LogEvent("*", $"{firstName} checked you out");
                if (CharData.ObserverSettings.ChatNotification)
                    SendChat($"♥ {pc.Name} is checking you out!");
                if (CharData.ObserverSettings.PlaySound)
                    PlayTargetSound();
                SaveCharData();
            }
            else if (!targeting && currentlyTargetingMe.Contains(pc.GameObjectId))
            {
                currentlyTargetingMe.Remove(pc.GameObjectId);
                CharData.CurrentlyTargeting.Remove(pName);
            }
            return targeting;
        }

        public Vector4 GetThemeColor() => Config.Theme switch
        {
            var t when t.StartsWith("Pretty in Pink") => new(0.98f, 0.25f, 0.58f, 1f),
            var t when t.StartsWith("Glamourous Green") => new(0.22f, 0.82f, 0.50f, 1f),
            var t when t.StartsWith("Beautifully Blue") => new(0.28f, 0.60f, 1.00f, 1f),
            _ => new(0.55f, 0.36f, 0.96f, 1f),
        };

        private void SendChat(string msg) => ChatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Echo,
            Name = "Memento",
            Message = new SeString(new TextPayload(msg))
        });

        // ════════════════════════════════════════════════════════════════
        // PER-CHARACTER DATA MANAGEMENT
        // ════════════════════════════════════════════════════════════════

        private string CharDataPath(ulong contentId) =>
            System.IO.Path.Combine(PluginInterface.ConfigDirectory.FullName, $"{contentId}.json");

        public void SaveCharData()
        {
            if (Config.DevMode || CharData == null) return;
            CharData.Save(CharDataPath(CharData.ContentId));
        }

        private void CheckCharacterSwitch()
        {
            var contentId = PlayerState.ContentId;
            if (contentId == 0 || contentId == currentContentId) return;

            SaveCharData();

            CharData = CharacterData.Load(CharDataPath(contentId), contentId);
            currentContentId = contentId;

            var player = ObjectTable.LocalPlayer;
            if (player != null)
                CharData.CharacterName = player.Name.ToString();

            PetManager = new PetManager(this);
        }

        private void MigrateGlobalToCharData()
        {
            if (Config.Version >= 8) return;

            var contentId = PlayerState.ContentId;
            var path = CharDataPath(contentId > 0 ? contentId : 1);

            if (!System.IO.File.Exists(path))
            {
                var migrated = new CharacterData
                {
                    ContentId = contentId,
                    SocialLog = Config.SocialLog,
                    EventLog = Config.EventLog,
                    EmoteCounts = Config.EmoteCounts,
                    AdmirerCounts = Config.AdmirerCounts,
                    TargetCounts = Config.TargetCounts,
                    TargetToday = Config.TargetToday,
                    TargetDayReset = Config.TargetDayReset,
                    ObserverSettings = Config.ObserverSettings,
                    CurrentPet = Config.CurrentPet,
                    PetHistory = Config.PetHistory,
                };
                migrated.Save(path);
            }

            Config.SocialLog = new();
            Config.EventLog = new();
            Config.EmoteCounts = new();
            Config.AdmirerCounts = new();
            Config.TargetCounts = new();
            Config.TargetToday = new();
            Config.CurrentPet = null;
            Config.PetHistory = new();
            Config.ObserverSettings = new();
            Config.Version = 8;
            Config.Save();
        }

        private void PlayTargetSound()
        {
            try
            {
                unsafe
                {
                    var addon = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Instance()
                        ->RaptureAtkUnitManager->GetAddonByName("_ToDoList");
                    if (addon != null)
                        addon->PlaySoundEffect(CharData.ObserverSettings.SoundEffectId);
                }
            }
            catch { }
        }

        private void DrawTargetDots()
        {
            if (!CharData.ObserverSettings.DrawDotOverhead || currentlyTargetingMe.Count == 0) return;

            var dl = ImGui.GetForegroundDrawList();
            foreach (var objId in currentlyTargetingMe)
            {
                var obj = ObjectTable.SearchById(objId);
                if (obj == null || obj.Address == IntPtr.Zero) continue;

                var obs = CharData.ObserverSettings;
                var dotColor = new Vector4(obs.DotColorR, obs.DotColorG, obs.DotColorB, 1f);

                var worldPos = obj.Position;
                worldPos.Y += obs.DotYOffset;

                if (GameGui.WorldToScreen(worldPos, out var screenPos))
                {
                    float r = obs.DotRadius;
                    dl.AddCircleFilled(screenPos, r + 3f, MementoColors.ToU32(dotColor, 0.25f));
                    dl.AddCircleFilled(screenPos, r, MementoColors.ToU32(dotColor, 0.90f));
                    dl.AddCircleFilled(screenPos, r * 0.4f,
                        MementoColors.ToU32(new Vector4(1f, 1f, 1f, 0.80f)));
                }
            }
        }

        public void Dispose()
        {
            SaveCharData();
            Config.Save();
            DutyState.DutyCompleted -= OnDutyCompleted;
            Framework.Update -= OnUpdate;
            ClientState.TerritoryChanged -= OnZoneChange;
            ChatGui.ChatMessage -= OnChatMessage;
            PluginInterface.UiBuilder.Draw -= drawAction;
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler("/memento");
            CommandManager.RemoveHandler("/mementopet");
        }
    }
}
