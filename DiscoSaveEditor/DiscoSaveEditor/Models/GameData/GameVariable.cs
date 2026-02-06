using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.GameData;

/// <summary>
/// Variable definition from variables_*.json files.
/// </summary>
public class GameVariable
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("initial_value")] public string InitialValue { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

/// <summary>
/// XP variable with point value from variables_xp.json.
/// </summary>
public class GameVariableXp : GameVariable
{
    [JsonPropertyName("xp_points")] public string XpPoints { get; set; } = "";
}
