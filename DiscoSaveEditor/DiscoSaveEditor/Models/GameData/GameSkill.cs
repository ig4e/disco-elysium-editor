using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.GameData;

/// <summary>
/// Skill/attribute definition from actors_skills.json.
/// </summary>
public class GameSkill
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("long_description")] public string LongDescription { get; set; } = "";
    [JsonPropertyName("attribute_group")] public string AttributeGroup { get; set; } = "";
    [JsonPropertyName("is_attribute")] public bool IsAttribute { get; set; }
}
