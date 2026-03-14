using Dalamud.Configuration;
namespace Memento;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Using proper Lists to ensure JSON saves correctly!
    public List<string> SocialLog { get; set; } = new List<string>();
    public List<string> TargetHistory { get; set; } = new List<string>();
    public List<string> TrackedEmotes { get; set; } = new List<string>() { "Dote", "Pet", "Embrace", "Blow Kiss"};

    // Chat Notification Settings
    public bool ShowEmoteChat { get; set; } = true;
    public bool ShowTargetChat { get; set; } = false;

    // Counts for stats
    public Dictionary<string, int> EmoteCounts { get; set; } = new();
    public Dictionary<string, int> AdmirerCounts { get; set; } = new();

    // Also add a counter for "Checking out" (Targeting)
    public Dictionary<string, int> TargetCounts { get; set; } = new();

    // UI Settings
    public string Theme { get; set; } = "Pretty in Pink";
    public float FontScale { get; set; } = 1.0f;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
