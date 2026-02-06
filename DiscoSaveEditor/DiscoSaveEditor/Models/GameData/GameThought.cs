using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.GameData;

/// <summary>
/// Thought definition from items_thoughts.json.
/// </summary>
public class GameThought
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("thought_type")] public string ThoughtType { get; set; } = "";
    [JsonPropertyName("bonus_while_processing")] public string BonusWhileProcessing { get; set; } = "";
    [JsonPropertyName("bonus_when_completed")] public string BonusWhenCompleted { get; set; } = "";
    [JsonPropertyName("completion_description")] public string CompletionDescription { get; set; } = "";
    [JsonPropertyName("time_to_internalize")] public double TimeToInternalize { get; set; }
    [JsonPropertyName("requirement")] public string Requirement { get; set; } = "";
    [JsonPropertyName("is_cursed")] public bool IsCursed { get; set; }
}
