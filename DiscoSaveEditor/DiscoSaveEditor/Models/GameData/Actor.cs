using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.GameData;

/// <summary>
/// Actor/NPC definition from actors_npcs_*.json files.
/// </summary>
public class Actor
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("long_description")] public string LongDescription { get; set; } = "";
    [JsonPropertyName("short_name")] public string ShortName { get; set; } = "";
    [JsonPropertyName("portrait")] public string Portrait { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
}
