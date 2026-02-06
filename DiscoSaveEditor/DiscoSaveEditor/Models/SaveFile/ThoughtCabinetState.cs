using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

public class ThoughtCabinetState
{
    [JsonPropertyName("thoughtListState")] public List<ThoughtState> ThoughtListState { get; set; } = new();
    [JsonPropertyName("thoughtCabinetViewState")] public ThoughtCabinetViewState ThoughtCabinetViewState { get; set; } = new();
}

public class ThoughtState
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("isFresh")] public bool IsFresh { get; set; }
    [JsonPropertyName("state")] public string State { get; set; } = "UNKNOWN";
    [JsonPropertyName("timeLeft")] public double TimeLeft { get; set; }
}

public class ThoughtCabinetViewState
{
    [JsonPropertyName("slotStates")] public List<SlotState> SlotStates { get; set; } = new();
    [JsonPropertyName("selectedProjectName")] public string SelectedProjectName { get; set; } = "";
}

public class SlotState
{
    [JsonPropertyName("Item1")] public string Item1 { get; set; } = "";
    [JsonPropertyName("Item2")] public string? Item2 { get; set; }
}
