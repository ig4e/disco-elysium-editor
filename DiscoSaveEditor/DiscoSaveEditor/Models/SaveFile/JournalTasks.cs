using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

public class AcquiredJournalTasks
{
    [JsonPropertyName("TaskAquisitions")] public Dictionary<string, GameTimestamp> TaskAcquisitions { get; set; } = new();
    [JsonPropertyName("TaskResolutions")] public Dictionary<string, JsonElement> TaskResolutions { get; set; } = new();
    [JsonPropertyName("SubtaskAquisitions")] public Dictionary<string, Dictionary<string, GameTimestamp>> SubtaskAcquisitions { get; set; } = new();
    [JsonPropertyName("TaskNewStates")] public Dictionary<string, bool> TaskNewStates { get; set; } = new();
    [JsonPropertyName("ChecksWithNotifications")] public JsonElement ChecksWithNotifications { get; set; }
    [JsonPropertyName("LastActiveTask")] public string LastActiveTask { get; set; } = "";
    [JsonPropertyName("LastDoneTask")] public string LastDoneTask { get; set; } = "";
    [JsonPropertyName("TasksTabNotifyIcon")] public bool TasksTabNotifyIcon { get; set; }
    [JsonPropertyName("ChecksTabNotifyIcon")] public bool ChecksTabNotifyIcon { get; set; }
    [JsonPropertyName("ActiveTasksTabNotifyIcon")] public bool ActiveTasksTabNotifyIcon { get; set; }
    [JsonPropertyName("DoneTasksTabNotifyIcon")] public bool DoneTasksTabNotifyIcon { get; set; }
    [JsonPropertyName("HudNotifyIcon")] public bool HudNotifyIcon { get; set; }
    [JsonPropertyName("wasChurchVisited")] public bool WasChurchVisited { get; set; }
    [JsonPropertyName("wasFishingVillageVisited")] public bool WasFishingVillageVisited { get; set; }
    [JsonPropertyName("wasQuicktravelChurchDiscovered")] public bool WasQuicktravelChurchDiscovered { get; set; }
    [JsonPropertyName("wasQuicktravelFishingVillageDiscovered")] public bool WasQuicktravelFishingVillageDiscovered { get; set; }
}
