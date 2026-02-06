using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Failed White Checks page.
/// Allows viewing and resetting failed passive skill checks to retry them.
/// </summary>
public partial class WhiteChecksViewModel : ObservableObject
{
    private readonly GameDataService _gameData;

    public ObservableCollection<WhiteCheckDisplayItem> FailedChecks { get; } = new();
    public ObservableCollection<WhiteCheckDisplayItem> SeenChecks { get; } = new();

    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial int TotalFailed { get; set; }
    [ObservableProperty] public partial int TotalSeen { get; set; }

    public WhiteChecksViewModel(GameDataService gameData)
    {
        _gameData = gameData;
        SearchQuery = "";
    }

    partial void OnSearchQueryChanged(string value)
    {
        // Filtering is applied during display - just reload
        LoadDisplay();
    }

    public void LoadFromSave(SaveData save)
    {
        var holder = save.Second.FailedWhiteChecksHolder;
        FailedChecks.Clear();
        SeenChecks.Clear();

        // Parse WhiteCheckCache (failed checks stored as JSON)
        foreach (var (key, element) in holder.WhiteCheckCache)
        {
            try
            {
                var check = System.Text.Json.JsonSerializer.Deserialize<WhiteCheck>(
                    element.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (check != null)
                {
                    FailedChecks.Add(new WhiteCheckDisplayItem
                    {
                        Key = key,
                        FlagName = check.FlagName,
                        SkillType = check.SkillType,
                        SkillDisplayName = _gameData.GetSkillDisplayNameByType(check.SkillType),
                        Difficulty = check.Difficulty,
                        LastSkillValue = check.LastSkillValue,
                        LastTargetValue = check.LastTargetValue,
                        CheckPrecondition = check.CheckPrecondition,
                        MarkedForReset = false
                    });
                }
            }
            catch { /* skip malformed entries */ }
        }

        // Parse SeenWhiteCheckCache
        foreach (var (key, check) in holder.SeenWhiteCheckCache)
        {
            SeenChecks.Add(new WhiteCheckDisplayItem
            {
                Key = key,
                FlagName = check.FlagName,
                SkillType = check.SkillType,
                SkillDisplayName = _gameData.GetSkillDisplayNameByType(check.SkillType),
                Difficulty = check.Difficulty,
                LastSkillValue = check.LastSkillValue,
                LastTargetValue = check.LastTargetValue,
                CheckPrecondition = check.CheckPrecondition,
                IsSeenOnly = check.IsOnlySeen,
                MarkedForReset = false
            });
        }

        TotalFailed = FailedChecks.Count;
        TotalSeen = SeenChecks.Count;
        LoadDisplay();
    }

    private void LoadDisplay()
    {
        // Search filtering is handled by the collections themselves
        // The UI binds directly to FailedChecks/SeenChecks
    }

    public void ApplyToSave(SaveData save)
    {
        var holder = save.Second.FailedWhiteChecksHolder;

        // Remove checks marked for reset from the cache
        foreach (var check in FailedChecks.Where(c => c.MarkedForReset))
        {
            holder.WhiteCheckCache.Remove(check.Key);
            // Also remove from ChecksBySkill if present
            if (holder.ChecksBySkill.ContainsKey(check.SkillType))
                holder.ChecksBySkill.Remove(check.SkillType);
        }

        foreach (var check in SeenChecks.Where(c => c.MarkedForReset))
        {
            holder.SeenWhiteCheckCache.Remove(check.Key);
        }
    }

    [RelayCommand]
    private void ToggleReset(WhiteCheckDisplayItem check)
    {
        check.MarkedForReset = !check.MarkedForReset;
    }

    [RelayCommand]
    private void ResetAllFailed()
    {
        foreach (var check in FailedChecks)
            check.MarkedForReset = true;
    }

    [RelayCommand]
    private void ClearResetMarks()
    {
        foreach (var check in FailedChecks)
            check.MarkedForReset = false;
        foreach (var check in SeenChecks)
            check.MarkedForReset = false;
    }
}

public partial class WhiteCheckDisplayItem : ObservableObject
{
    public string Key { get; set; } = "";
    public string FlagName { get; set; } = "";
    public string SkillType { get; set; } = "";
    public string SkillDisplayName { get; set; } = "";
    public int Difficulty { get; set; }
    public int LastSkillValue { get; set; }
    public int LastTargetValue { get; set; }
    public string CheckPrecondition { get; set; } = "";
    public bool IsSeenOnly { get; set; }

    [ObservableProperty] public partial bool MarkedForReset { get; set; }

    public string DifficultyText => $"DC {Difficulty} (Rolled: {LastSkillValue} vs {LastTargetValue})";
    public string StatusText => MarkedForReset ? "Will be RESET" : "Failed";
}
