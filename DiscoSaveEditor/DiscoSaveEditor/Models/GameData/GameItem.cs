using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.GameData;

/// <summary>
/// Item definition from items_inventory.json.
/// </summary>
public class GameItem
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("item_type")] public double ItemType { get; set; }
    [JsonPropertyName("item_group")] public double ItemGroup { get; set; }
    [JsonPropertyName("item_value")] public double ItemValue { get; set; }
    [JsonPropertyName("bonus")] public string Bonus { get; set; } = "";
    [JsonPropertyName("MediumTextValue")] public string MediumTextValue { get; set; } = "";
    [JsonPropertyName("is_quest_item")] public bool IsQuestItem { get; set; }
    [JsonPropertyName("autoequip")] public string Autoequip { get; set; } = "";
    [JsonPropertyName("cursed")] public string Cursed { get; set; } = "";
    [JsonPropertyName("isSubstance")] public string IsSubstance { get; set; } = "";
    [JsonPropertyName("isConsumable")] public string IsConsumable { get; set; } = "";
    [JsonPropertyName("multipleAllowed")] public string MultipleAllowed { get; set; } = "";

    [JsonIgnore] public bool IsCursed => string.Equals(Cursed, "True", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore] public bool IsAutoequip => string.Equals(Autoequip, "True", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore] public bool IsSubstanceItem => string.Equals(IsSubstance, "True", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore] public bool IsConsumableItem => string.Equals(IsConsumable, "True", StringComparison.OrdinalIgnoreCase);
}
