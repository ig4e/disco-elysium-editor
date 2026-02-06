using System.Reflection;
using System.Text.Json;
using DiscoSaveEditor.Models.GameData;

namespace DiscoSaveEditor.Services;

/// <summary>
/// Loads static game definition data from embedded JSON resources.
/// Singleton — loaded once at startup.
/// </summary>
public class GameDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Lookups by name
    public Dictionary<string, GameSkill> Skills { get; private set; } = new();
    public Dictionary<string, GameItem> Items { get; private set; } = new();
    public Dictionary<string, GameThought> Thoughts { get; private set; } = new();
    public Dictionary<string, GameVariable> TaskVariables { get; private set; } = new();
    public Dictionary<string, GameVariable> ReputationVariables { get; private set; } = new();
    public Dictionary<string, GameVariable> AllVariables { get; private set; } = new();
    public Dictionary<string, GameVariable> CharacterVariables { get; private set; } = new();
    public Dictionary<string, GameVariableXp> XpVariables { get; private set; } = new();
    public Dictionary<string, Actor> MajorNpcs { get; private set; } = new();
    public SkillKeyMap SkillKeyMap { get; private set; } = new();

    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Load all game data from the Assets/GameData folder at the given base path.
    /// </summary>
    public async Task LoadAsync(string gameDataFolder)
    {
        Skills = (await LoadJsonArrayAsync<GameSkill>(Path.Combine(gameDataFolder, "actors_skills.json")))
            .ToDictionary(s => s.Name);

        Items = (await LoadJsonArrayAsync<GameItem>(Path.Combine(gameDataFolder, "items_inventory.json")))
            .ToDictionary(i => i.Name);

        Thoughts = (await LoadJsonArrayAsync<GameThought>(Path.Combine(gameDataFolder, "items_thoughts.json")))
            .ToDictionary(t => t.Name);

        TaskVariables = (await LoadJsonArrayAsync<GameVariable>(Path.Combine(gameDataFolder, "variables_tasks.json")))
            .ToDictionary(v => v.Name);

        ReputationVariables = (await LoadJsonArrayAsync<GameVariable>(Path.Combine(gameDataFolder, "variables_reputation.json")))
            .ToDictionary(v => v.Name);

        CharacterVariables = (await LoadJsonArrayAsync<GameVariable>(Path.Combine(gameDataFolder, "variables_character.json")))
            .ToDictionary(v => v.Name);

        AllVariables = (await LoadJsonArrayAsync<GameVariable>(Path.Combine(gameDataFolder, "variables_all.json")))
            .ToDictionary(v => v.Name);

        XpVariables = (await LoadJsonArrayAsync<GameVariableXp>(Path.Combine(gameDataFolder, "variables_xp.json")))
            .ToDictionary(v => v.Name);

        MajorNpcs = (await LoadJsonArrayAsync<Actor>(Path.Combine(gameDataFolder, "actors_npcs_major.json")))
            .ToDictionary(a => a.Name);

        var keyMapPath = Path.Combine(gameDataFolder, "skill_key_map.json");
        var keyMapJson = await File.ReadAllTextAsync(keyMapPath);
        SkillKeyMap = JsonSerializer.Deserialize<SkillKeyMap>(keyMapJson, JsonOptions) ?? new();

        IsLoaded = true;
    }

    private static async Task<List<T>> LoadJsonArrayAsync<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<T>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
    }

    /// <summary>Get display name for a skill save key (e.g. "visualCalculus" → "Visual Calculus")</summary>
    public string GetSkillDisplayName(string saveKey) =>
        SkillKeyMap.FindBySaveKey(saveKey)?.DisplayName ?? saveKey;

    /// <summary>Get display name for a skill type code (e.g. "VISUAL_CALCULUS" → "Visual Calculus")</summary>
    public string GetSkillDisplayNameByType(string skillType) =>
        SkillKeyMap.FindBySkillType(skillType)?.DisplayName ?? skillType;

    /// <summary>Get item definition by name</summary>
    public GameItem? GetItem(string name) => Items.GetValueOrDefault(name);

    /// <summary>Get thought definition by name</summary>
    public GameThought? GetThought(string name) => Thoughts.GetValueOrDefault(name);

    /// <summary>Get task variable description</summary>
    public string GetTaskDescription(string taskName) =>
        TaskVariables.GetValueOrDefault(taskName)?.Description ?? taskName;
}
