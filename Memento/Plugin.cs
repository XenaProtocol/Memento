using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Memento
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Memento";

        // --- DALAMUD SERVICES ---
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!; // Added for chat messages!

        // --- PLUGIN STATE & VARIABLES ---
        public Configuration Config { get; private set; }
        private MainWindow mainWindow;
        private bool isUiVisible = false;
        private string emoteSearch = "";
        private uint selectedEmoteId = 0;

        // Temporary Memory (Resets on log in)
        public HashSet<ulong> currentlyTargetingMe = new();
        private Dictionary<ulong, ushort> lastEmoteIds = new();


        public Plugin()
        {
            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);
            Config.TrackedEmotes = Config.TrackedEmotes.Distinct().ToList();
            Config.Save();

            // Create our new window!
            mainWindow = new MainWindow(this);

            CommandManager.AddHandler("/memento", new CommandInfo((c, a) => mainWindow.IsVisible = !mainWindow.IsVisible)
            {
                HelpMessage = "Opens the Tracker UI"
            });

            // Hook up the events to our new window
            PluginInterface.UiBuilder.Draw += mainWindow.DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += () => mainWindow.IsVisible = true;
            PluginInterface.UiBuilder.OpenConfigUi += () => mainWindow.IsVisible = true;

            Framework.Update += OnUpdate;
        }

        public void Dispose()
        {
            Config.Save();
            Framework.Update -= OnUpdate;
            PluginInterface.UiBuilder.Draw -= mainWindow.DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= () => mainWindow.IsVisible = true;
            PluginInterface.UiBuilder.OpenConfigUi -= () => mainWindow.IsVisible = true;
            CommandManager.RemoveHandler("/memento");
        }

        private void OnUpdate(IFramework framework)
        {
            var localPlayer = ClientState.LocalPlayer;

            if (localPlayer == null || localPlayer.Address == IntPtr.Zero) return;
            var myId = localPlayer.GameObjectId;

            foreach (var obj in ObjectTable)
            {
                if (obj is IPlayerCharacter pc && pc.Address != IntPtr.Zero && pc.GameObjectId != myId)
                {
                    var isTargetingMe = targetTracking(myId, pc);

                    // --- 2. EMOTE TRACKING ---
                    var flowControl = emoteTracking(pc, isTargetingMe);
                    if (!flowControl)
                    {
                        continue;
                    }
                }
            }
        }

        private bool emoteTracking(IPlayerCharacter pc, bool isTargetingMe)
        {
            unsafe
            {
                var character = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)pc.Address;
                if (character == null) return false;

                try
                {
                    ushort currentEmoteId = character->EmoteController.EmoteId;
                    lastEmoteIds.TryGetValue(pc.GameObjectId, out var lastId);

                    if (currentEmoteId != 0 && currentEmoteId != 243 && currentEmoteId != lastId)
                    {
                        var row = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Emote>()?.GetRow(currentEmoteId);
                        if (row.HasValue)
                        {
                            string emoteName = row.Value.Name.ToString();

                            if (!string.IsNullOrEmpty(emoteName) && Config.TrackedEmotes.Contains(emoteName))
                            {
                                if (isTargetingMe)
                                {
                                    string entry = $"[{DateTime.Now:t}] {pc.Name} used {emoteName} on you!";
                                    Config.SocialLog.Insert(0, entry);

                                    if (Config.SocialLog.Count > 100) Config.SocialLog.RemoveAt(100);
                                    Config.EmoteCounts[emoteName] = Config.EmoteCounts.GetValueOrDefault(emoteName) + 1;

                                    var playerName = pc.Name.ToString();
                                    Config.AdmirerCounts[playerName] = Config.AdmirerCounts.GetValueOrDefault(playerName) + 1;
                                    Config.Save(); // Save!

                                    // Optional Chat Notification
                                    if (Config.ShowEmoteChat) SendChatNotification($"♥ {pc.Name} used {emoteName} on you!");
                                }
                            }
                        }
                    }
                    lastEmoteIds[pc.GameObjectId] = currentEmoteId;
                }
                catch (Exception) { return false; }
            }

            return true;
        }

        private bool targetTracking(ulong myId, IPlayerCharacter pc)
        {
            // --- 1. TARGET TRACKING ---
            bool isTargetingMe = pc.TargetObjectId == myId;
            if (isTargetingMe && !currentlyTargetingMe.Contains(pc.GameObjectId))
            {
                currentlyTargetingMe.Add(pc.GameObjectId);
                Config.TargetHistory.Insert(0, $"[{DateTime.Now:t}] {pc.Name} is checking you out!");

                if (Config.TargetHistory.Count > 100) Config.TargetHistory.RemoveAt(100);
                Config.Save(); // Save!

                // Optional Chat Notification
                if (Config.ShowTargetChat) SendChatNotification($"♥ {pc.Name} is checking you out!");
                var name = pc.Name.ToString();
                Config.TargetCounts[name] = Config.TargetCounts.GetValueOrDefault(name) + 1;
                Config.Save();

            }
            else if (!isTargetingMe && currentlyTargetingMe.Contains(pc.GameObjectId))
            {
                currentlyTargetingMe.Remove(pc.GameObjectId);
            }

            return isTargetingMe;
        }

        private void SendChatNotification(string message)
        {
            ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Echo,
                Name = "Memento",
                Message = new SeString(new TextPayload(message))
            });
        }

        
    }
}
