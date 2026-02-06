using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.GameData;

/// <summary>
/// Mapping between save file keys, internal type codes, and display names.
/// Loaded from skill_key_map.json.
/// </summary>
public class SkillKeyMap
{
    [JsonPropertyName("abilities")] public List<AbilityMapping> Abilities { get; set; } = new();
    [JsonPropertyName("skills")] public List<SkillMapping> Skills { get; set; } = new();
    [JsonPropertyName("equipment_slots")] public List<string> EquipmentSlots { get; set; } = new();
    [JsonPropertyName("inventory_categories")] public List<string> InventoryCategories { get; set; } = new();
    [JsonPropertyName("modifier_cause_types")] public List<string> ModifierCauseTypes { get; set; } = new();
    [JsonPropertyName("thought_states")] public List<string> ThoughtStates { get; set; } = new();
    [JsonPropertyName("healing_pool_types")] public List<string> HealingPoolTypes { get; set; } = new();

    // Lookup helpers
    private Dictionary<string, SkillMapping>? _bySaveKey;
    private Dictionary<string, SkillMapping>? _bySkillType;

    public SkillMapping? FindBySaveKey(string saveKey) =>
        (_bySaveKey ??= Skills.ToDictionary(s => s.SaveKey)).GetValueOrDefault(saveKey);

    public SkillMapping? FindBySkillType(string skillType) =>
        (_bySkillType ??= Skills.ToDictionary(s => s.SkillType)).GetValueOrDefault(skillType);

    public AbilityMapping? FindAbilityBySaveKey(string saveKey) =>
        Abilities.FirstOrDefault(a => a.SaveKey == saveKey);

    public HashSet<string> GetAbilityKeys() => Abilities.Select(a => a.SaveKey).ToHashSet();
    public HashSet<string> GetSkillKeys() => Skills.Select(s => s.SaveKey).ToHashSet();
}

public class AbilityMapping
{
    [JsonPropertyName("save_key")] public string SaveKey { get; set; } = "";
    [JsonPropertyName("skill_type")] public string SkillType { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
}

public class SkillMapping
{
    [JsonPropertyName("save_key")] public string SaveKey { get; set; } = "";
    [JsonPropertyName("skill_type")] public string SkillType { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("ability")] public string Ability { get; set; } = "";
}
