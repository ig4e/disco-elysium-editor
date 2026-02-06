using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

public class FailedWhiteChecksHolder
{
    [JsonPropertyName("ReopenedWhiteChecksByActorName")] public Dictionary<string, JsonElement> ReopenedWhiteChecksByActorName { get; set; } = new();
    [JsonPropertyName("WhiteCheckCache")] public Dictionary<string, JsonElement> WhiteCheckCache { get; set; } = new();
    [JsonPropertyName("SeenWhiteCheckCache")] public Dictionary<string, WhiteCheck> SeenWhiteCheckCache { get; set; } = new();
    [JsonPropertyName("ChecksBySkill")] public Dictionary<string, JsonElement> ChecksBySkill { get; set; } = new();
    [JsonPropertyName("ChecksByVariable")] public Dictionary<string, JsonElement> ChecksByVariable { get; set; } = new();
}

public class WhiteCheck
{
    [JsonPropertyName("FlagName")] public string FlagName { get; set; } = "";
    [JsonPropertyName("SkillType")] public string SkillType { get; set; } = "";
    [JsonPropertyName("LastSkillValue")] public int LastSkillValue { get; set; }
    [JsonPropertyName("LastTargetValue")] public int LastTargetValue { get; set; }
    [JsonPropertyName("difficulty")] public int Difficulty { get; set; }
    [JsonPropertyName("checkPrecondition")] public string CheckPrecondition { get; set; } = "";
    [JsonPropertyName("isOnlySeen")] public bool IsOnlySeen { get; set; }
    [JsonPropertyName("checkTargetArticyId")] public string CheckTargetArticyId { get; set; } = "";
    [JsonPropertyName("Actor")] public JsonElement? Actor { get; set; }
    [JsonPropertyName("CheckModifiers")] public Dictionary<string, List<CheckModifier>>? CheckModifiers { get; set; }
}

public class CheckModifier
{
    [JsonPropertyName("expression")] public string Expression { get; set; } = "";
    [JsonPropertyName("bonus")] public int Bonus { get; set; }
    [JsonPropertyName("fallbackExplanation")] public string FallbackExplanation { get; set; } = "";
}
