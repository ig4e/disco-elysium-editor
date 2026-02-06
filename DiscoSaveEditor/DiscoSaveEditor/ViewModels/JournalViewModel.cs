using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Journal/Quests page.
/// Shows acquired tasks, their resolution status, and subtasks.
/// </summary>
public partial class JournalViewModel : ObservableObject
{
    private readonly GameDataService _gameData;
    private readonly List<TaskDisplayItem> _allTasks = new();

    public ObservableCollection<TaskDisplayItem> ActiveTasks { get; } = new();
    public ObservableCollection<TaskDisplayItem> CompletedTasks { get; } = new();

    [ObservableProperty] public partial string SearchQuery { get; set; }

    public JournalViewModel(GameDataService gameData)
    {
        _gameData = gameData;
        SearchQuery = "";
    }

    partial void OnSearchQueryChanged(string value)
    {
        RefreshFilteredTasks();
    }

    private void RefreshFilteredTasks()
    {
        ActiveTasks.Clear();
        CompletedTasks.Clear();
        var q = SearchQuery?.ToLower() ?? "";

        foreach (var item in _allTasks)
        {
            if (!string.IsNullOrEmpty(q) &&
                !item.Description.ToLower().Contains(q) &&
                !item.TaskName.ToLower().Contains(q))
                continue;

            if (item.IsResolved)
                CompletedTasks.Add(item);
            else
                ActiveTasks.Add(item);
        }
    }

    public void LoadFromSave(SaveData save)
    {
        var journal = save.Second.AcquiredJournalTasks;
        _allTasks.Clear();

        foreach (var (taskName, acquisition) in journal.TaskAcquisitions)
        {
            var description = _gameData.GetTaskDescription(taskName);
            var resolution = journal.TaskResolutions.GetValueOrDefault(taskName);
            var isResolved = resolution.ValueKind == System.Text.Json.JsonValueKind.Object
                && resolution.EnumerateObject().Any();
            var isNew = journal.TaskNewStates.GetValueOrDefault(taskName) == false;

            // Get subtasks
            var subtasks = new List<string>();
            if (journal.SubtaskAcquisitions.TryGetValue(taskName, out var subs))
            {
                subtasks = subs.Keys.Select(k => _gameData.GetTaskDescription(k)).ToList();
            }

            _allTasks.Add(new TaskDisplayItem
            {
                TaskName = taskName,
                Description = description,
                AcquiredTime = acquisition.ToString(),
                OriginalTimestamp = acquisition,
                OriginalResolution = resolution,
                IsResolved = isResolved,
                IsNew = isNew,
                Subtasks = subtasks
            });
        }

        RefreshFilteredTasks();
    }

    public void ApplyToSave(SaveData save)
    {
        var journal = save.Second.AcquiredJournalTasks;

        // Rebuild TaskAcquisitions — ALL tasks keep their acquisition timestamp
        journal.TaskAcquisitions.Clear();
        foreach (var task in _allTasks)
        {
            journal.TaskAcquisitions[task.TaskName] = task.OriginalTimestamp ??
                new GameTimestamp { DayCounter = 1, DayMinutes = 0, Seconds = 0 };
        }

        // Rebuild TaskResolutions — resolved tasks get their resolution data, unresolved get empty
        journal.TaskResolutions.Clear();
        foreach (var task in _allTasks)
        {
            if (task.IsResolved)
            {
                journal.TaskResolutions[task.TaskName] = task.OriginalResolution.HasValue
                    && task.OriginalResolution.Value.ValueKind == System.Text.Json.JsonValueKind.Object
                    && task.OriginalResolution.Value.EnumerateObject().Any()
                    ? task.OriginalResolution.Value
                    : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{\"resolution\": 1}");
            }
            // Unresolved tasks simply don't appear in TaskResolutions
        }

        // Update new states
        foreach (var task in _allTasks)
        {
            journal.TaskNewStates[task.TaskName] = !task.IsNew;
        }
    }

    [RelayCommand]
    private void ResolveAll()
    {
        foreach (var task in _allTasks)
            task.IsResolved = true;
        RefreshFilteredTasks();
    }

    [RelayCommand]
    private void UnresolveAll()
    {
        foreach (var task in _allTasks)
            task.IsResolved = false;
        RefreshFilteredTasks();
    }
}

public partial class TaskDisplayItem : ObservableObject
{
    public string TaskName { get; set; } = "";
    public string Description { get; set; } = "";
    public string AcquiredTime { get; set; } = "";
    public List<string> Subtasks { get; set; } = new();
    
    public GameTimestamp? OriginalTimestamp { get; set; }
    public System.Text.Json.JsonElement? OriginalResolution { get; set; }

    [ObservableProperty] public partial bool IsResolved { get; set; }
    [ObservableProperty] public partial bool IsNew { get; set; }
}
