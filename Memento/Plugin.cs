using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;

namespace Memento
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Memento";

        // The WindowSystem manages the lifecycle and Z-order of our windows
        public WindowSystem WindowSystem = new("Memento");

        // --- DALAMUD SERVICES ---
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

        // --- PLUGIN STATE & VARIABLES ---
        public Configuration Config { get; private set; }

        // Window instances
        private MainWindow mainWindow;
        public ConfigWindow configWindow;

        // Named action for the Draw hook so we can safely unsubscribe in Dispose
        private readonly Action drawAction;

        // Temporary Memory (Resets on log in)
        public HashSet<ulong> currentlyTargetingMe = new();
        private Dictionary<ulong, ushort> lastEmoteIds = new();

        public Plugin()
        {
            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);

            // 1. Initialize our sibling windows
            mainWindow = new MainWindow(this);
            configWindow = new ConfigWindow(this);

            // 2. Add them to the WindowSystem manager
            this.WindowSystem.AddWindow(mainWindow);
            this.WindowSystem.AddWindow(configWindow);

            // 3. Define the Draw Logic (Applying theme to the whole system)
            drawAction = () =>
            {
                using (var theme = new WindowCustomization(Config.Theme))
                {
                    this.WindowSystem.Draw();
                }
            };

            // 4. Hook up Events
            CommandManager.AddHandler("/memento", new CommandInfo((c, a) => mainWindow.IsOpen = !mainWindow.IsOpen)
            {
                HelpMessage = "Opens the Tracker UI"
            });

            PluginInterface.UiBuilder.Draw += drawAction;
            PluginInterface.UiBuilder.OpenMainUi += () => mainWindow.IsOpen = true;
            PluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;

            Framework.Update += OnUpdate;
        }

        public Vector4 GetThemeColor()
        {
            return Config.Theme switch
            {
                "Perfectly Purple" => new Vector4(0.6f, 0.4f, 0.9f, 1.0f),
                "Beautifully Blue" => new Vector4(0.3f, 0.5f, 0.8f, 1.0f), // Darker Sky
                "Glamourous Green" => new Vector4(0.2f, 0.6f, 0.4f, 1.0f), // Deep Mint
                _ => new Vector4(0.9f, 0.4f, 0.6f, 1.0f), // Default Pink
            };
        }

        public Vector4 GetSearchBgColor()
        {
            return Config.Theme switch
            {
                "Perfectly Purple" => new Vector4(0.88f, 0.85f, 0.92f, 1.0f), // Soft Lavender
                "Beautifully Blue" => new Vector4(0.85f, 0.90f, 0.94f, 1.0f), // Soft Sky
                "Glamourous Green" => new Vector4(0.85f, 0.92f, 0.88f, 1.0f), // Soft Mint
                _ => new Vector4(0.92f, 0.85f, 0.88f, 1.0f), // Your original subtle pink!
            };
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

                    // EMOTE TRACKING
                    if (!emoteTracking(pc, isTargetingMe))
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
                                    Config.Save();

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
            bool isTargetingMe = pc.TargetObjectId == myId;
            if (isTargetingMe && !currentlyTargetingMe.Contains(pc.GameObjectId))
            {
                currentlyTargetingMe.Add(pc.GameObjectId);
                Config.TargetHistory.Insert(0, $"[{DateTime.Now:t}] {pc.Name} is checking you out!");

                if (Config.TargetHistory.Count > 100) Config.TargetHistory.RemoveAt(100);

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

        public void Dispose()
        {
            Config.Save();

            // Unhook game events
            Framework.Update -= OnUpdate;

            // Properly unhook the named draw action and clear windows
            PluginInterface.UiBuilder.Draw -= drawAction;
            WindowSystem.RemoveAllWindows();

            CommandManager.RemoveHandler("/memento");
        }
    }
}
