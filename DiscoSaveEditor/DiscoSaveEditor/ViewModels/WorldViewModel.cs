using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the World page.
/// Manages the Lua database (12,000+ variables) and Fog of War.
/// </summary>
public partial class WorldViewModel : ObservableObject
{
    private readonly GameDataService _gameData;
    private Dictionary<string, object> _originalLuaDb = new();
    private Dictionary<string, object> _flattenedDb = new();
    /// <summary>Tracks all user edits by dotted key, persisted across searches</summary>
    private readonly Dictionary<string, string> _pendingEdits = new();

    public ObservableCollection<VariableDisplayItem> Variables { get; } = new();

    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial bool IsFogOfWarEnabled { get; set; }
    [ObservableProperty] public partial int TotalVariableCount { get; set; }

    // Reputation quick-access
    [ObservableProperty] public partial double ReputationCommunist { get; set; }
    [ObservableProperty] public partial double ReputationUltraliberal { get; set; }
    [ObservableProperty] public partial double ReputationMoralist { get; set; }
    [ObservableProperty] public partial double ReputationNationalist { get; set; }
    [ObservableProperty] public partial double ReputationKim { get; set; }

    // Weather
    [ObservableProperty] public partial int WeatherPreset { get; set; }
    public List<string> WeatherPresets { get; } = new()
    {
        "0 - Default / Clear",
        "1 - Overcast",
        "2 - Rain",
        "3 - Snow",
        "4 - Fog"
    };

    public WorldViewModel(GameDataService gameData)
    {
        _gameData = gameData;
        SearchQuery = "";
    }

    public void LoadFromSave(SaveData save)
    {
        _originalLuaDb = save.LuaDatabase;
        _flattenedDb = LuaDatabaseService.Flatten(_originalLuaDb);
        _pendingEdits.Clear();
        TotalVariableCount = _flattenedDb.Count;
        IsFogOfWarEnabled = save.First.FowUnrevealersStatusCache.Count > 0;

        // Weather
        WeatherPreset = save.Second.WeatherState.WeatherPreset;

        // Load reputation quick-access values
        ReputationCommunist = GetLuaDouble("reputation.communist");
        ReputationUltraliberal = GetLuaDouble("reputation.ultraliberal");
        ReputationMoralist = GetLuaDouble("reputation.moralist");
        ReputationNationalist = GetLuaDouble("reputation.revacholian_nationhood");
        ReputationKim = GetLuaDouble("reputation.kim");

        RefreshVariables();
    }

    private double GetLuaDouble(string key) =>
        _flattenedDb.TryGetValue(key, out var v) && v is double d ? d : 0.0;

    partial void OnSearchQueryChanged(string value)
    {
        // Save any pending edits from the current display before refreshing
        CaptureCurrentEdits();
        RefreshVariables();
    }

    /// <summary>Capture edits from the currently displayed variables</summary>
    private void CaptureCurrentEdits()
    {
        foreach (var item in Variables)
        {
            var originalValue = _flattenedDb.TryGetValue(item.Key, out var v) ? v?.ToString() ?? "" : "";
            if (item.Value != originalValue)
            {
                _pendingEdits[item.Key] = item.Value;
            }
        }
    }

    private void RefreshVariables()
    {
        Variables.Clear();
        var query = SearchQuery?.ToLower() ?? "";

        var filtered = _flattenedDb
            .Where(kvp => string.IsNullOrEmpty(query) || kvp.Key.ToLower().Contains(query))
            .OrderBy(kvp => kvp.Key)
            .Take(200);

        foreach (var kvp in filtered)
        {
            var meta = _gameData.AllVariables.GetValueOrDefault(kvp.Key);
            // Show pending edit value if one exists, otherwise the original
            var displayValue = _pendingEdits.TryGetValue(kvp.Key, out var edited)
                ? edited
                : kvp.Value?.ToString() ?? "";

            Variables.Add(new VariableDisplayItem
            {
                Key = kvp.Key,
                Value = displayValue,
                Type = GetTypeName(kvp.Value!),
                Description = meta?.Description ?? ""
            });
        }
    }

    private static string GetTypeName(object value) => value switch
    {
        double => "Number",
        bool => "Boolean",
        string => "String",
        _ => value?.GetType().Name ?? "Unknown"
    };

    public void ApplyToSave(SaveData save)
    {
        // Capture any final edits from the current display
        CaptureCurrentEdits();

        // Apply all pending edits to the original nested dictionary
        foreach (var (key, value) in _pendingEdits)
        {
            // Determine the original type and convert
            if (_flattenedDb.TryGetValue(key, out var original))
            {
                object typedValue;
                if (original is double)
                    typedValue = double.TryParse(value, out var d) ? d : 0.0;
                else if (original is bool)
                    typedValue = bool.TryParse(value, out var b) && b;
                else
                    typedValue = value;

                LuaDatabaseService.SetValue(save.LuaDatabase, key, typedValue);
            }
        }

        // Reputation quick-access
        LuaDatabaseService.SetValue(save.LuaDatabase, "reputation.communist", ReputationCommunist);
        LuaDatabaseService.SetValue(save.LuaDatabase, "reputation.ultraliberal", ReputationUltraliberal);
        LuaDatabaseService.SetValue(save.LuaDatabase, "reputation.moralist", ReputationMoralist);
        LuaDatabaseService.SetValue(save.LuaDatabase, "reputation.revacholian_nationhood", ReputationNationalist);
        LuaDatabaseService.SetValue(save.LuaDatabase, "reputation.kim", ReputationKim);

        // Weather
        save.Second.WeatherState.WeatherPreset = WeatherPreset;

        // Fog of War
        if (!IsFogOfWarEnabled)
        {
            save.First.FowUnrevealersStatusCache.Clear();
        }
    }
}

public partial class VariableDisplayItem : ObservableObject
{
    public string Key { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";

    [ObservableProperty] public partial string Value { get; set; }
}
