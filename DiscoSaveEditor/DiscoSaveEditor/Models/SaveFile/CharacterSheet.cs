using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

/// <summary>
/// The character sheet contains abilities, skills, item/thought arrays, and modifier maps.
/// In the JSON, ability/skill keys are mixed with collection fields — use CharacterSheetConverter to parse.
/// </summary>
public class CharacterSheet
{
    /// <summary>4 abilities keyed by save_key: intellect, psyche, fysique, motorics</summary>
    public Dictionary<string, SkillEntry> Abilities { get; set; } = new();

    /// <summary>24 skills keyed by save_key: logic, encyclopedia, etc.</summary>
    public Dictionary<string, SkillEntry> Skills { get; set; } = new();

    // Collection fields
    public List<string> GainedItems { get; set; } = new();
    public List<string> EquippedItems { get; set; } = new();
    public List<string> GainedThoughts { get; set; } = new();
    public List<string> CookingThoughts { get; set; } = new();
    public List<string> FixedThoughts { get; set; } = new();
    public List<string> ForgottenThoughts { get; set; } = new();
    public string SelectedPanelName { get; set; } = "";

    // Modifier maps — keyed by UPPER_CASE skill type
    public Dictionary<string, List<ModifierEntry>> SkillModifierCauseMap { get; set; } = new();
    public Dictionary<string, List<ModifierEntry>> AbilityModifierCauseMap { get; set; } = new();
}

public class SkillEntry
{
    [JsonPropertyName("skillType")] public string SkillType { get; set; } = "";
    [JsonPropertyName("abilityType")] public string AbilityType { get; set; } = "";
    [JsonPropertyName("dirty")] public bool Dirty { get; set; }
    [JsonPropertyName("value")] public int Value { get; set; }
    [JsonPropertyName("valueWithoutPerceptionsSubSkills")] public int ValueWithoutPerceptionsSubSkills { get; set; }
    [JsonPropertyName("damageValue")] public int DamageValue { get; set; }
    [JsonPropertyName("maximumValue")] public int MaximumValue { get; set; }
    [JsonPropertyName("calculatedAbility")] public int CalculatedAbility { get; set; }
    [JsonPropertyName("rankValue")] public int RankValue { get; set; }
    [JsonPropertyName("hasAdvancement")] public bool HasAdvancement { get; set; }
    [JsonPropertyName("isSignature")] public bool IsSignature { get; set; }
    [JsonPropertyName("modifiers")] public JsonElement? Modifiers { get; set; }
}

public class ModifierEntry
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = "";
    [JsonPropertyName("skillType")] public string SkillType { get; set; } = "";
    [JsonPropertyName("modifierCause")] public ModifierCause ModifierCause { get; set; } = new();
}

public class ModifierCause
{
    [JsonPropertyName("ModifierKey")] public string ModifierKey { get; set; } = "";
    [JsonPropertyName("ModifierCauseType")] public string ModifierCauseType { get; set; } = "";
}
