using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.GameData;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Thought Cabinet page.
/// Shows all 53 thoughts with their state (UNKNOWN/COOKING/FIXED/FORGOTTEN).
/// </summary>
public partial class ThoughtCabinetViewModel : ObservableObject
{
    private readonly GameDataService _gameData;

    public ObservableCollection<ThoughtDisplayItem> Thoughts { get; } = new();

    public ThoughtCabinetViewModel(GameDataService gameData)
    {
        _gameData = gameData;
    }

    public void LoadFromSave(SaveData save)
    {
        var cs = save.Second.CharacterSheet;
        var tcState = save.Second.ThoughtCabinetState;

        Thoughts.Clear();

        foreach (var thoughtDef in _gameData.Thoughts.Values.OrderBy(t => t.DisplayName))
        {
            var state = tcState.ThoughtListState.FirstOrDefault(t => t.Name == thoughtDef.Name);
            var currentState = "UNKNOWN";

            if (cs.FixedThoughts.Contains(thoughtDef.Name))
                currentState = "FIXED";
            else if (cs.CookingThoughts.Contains(thoughtDef.Name))
                currentState = "COOKING";
            else if (cs.ForgottenThoughts.Contains(thoughtDef.Name))
                currentState = "FORGOTTEN";
            else if (cs.GainedThoughts.Contains(thoughtDef.Name))
                currentState = "GAINED";

            Thoughts.Add(new ThoughtDisplayItem
            {
                Name = thoughtDef.Name,
                DisplayName = thoughtDef.DisplayName,
                Description = thoughtDef.Description,
                BonusWhileProcessing = thoughtDef.BonusWhileProcessing,
                BonusWhenCompleted = thoughtDef.BonusWhenCompleted,
                ThoughtType = thoughtDef.ThoughtType,
                TimeToInternalize = thoughtDef.TimeToInternalize,
                Requirement = thoughtDef.Requirement,
                IsCursed = thoughtDef.IsCursed,
                State = currentState,
                TimeLeft = state?.TimeLeft ?? 0
            });
        }
    }

    public void ApplyToSave(SaveData save)
    {
        var cs = save.Second.CharacterSheet;
        var tcState = save.Second.ThoughtCabinetState;

        cs.GainedThoughts.Clear();
        cs.CookingThoughts.Clear();
        cs.FixedThoughts.Clear();
        cs.ForgottenThoughts.Clear();

        var slotStates = new List<SlotState>();

        foreach (var thought in Thoughts)
        {
            switch (thought.State)
            {
                case "GAINED":
                    cs.GainedThoughts.Add(thought.Name);
                    break;
                case "COOKING":
                    cs.GainedThoughts.Add(thought.Name);
                    cs.CookingThoughts.Add(thought.Name);
                    break;
                case "FIXED":
                    cs.GainedThoughts.Add(thought.Name);
                    cs.FixedThoughts.Add(thought.Name);
                    slotStates.Add(new SlotState { Item1 = "FILLED", Item2 = thought.Name });
                    break;
                case "FORGOTTEN":
                    cs.GainedThoughts.Add(thought.Name);
                    cs.ForgottenThoughts.Add(thought.Name);
                    break;
            }

            // Update thought list state
            var existingState = tcState.ThoughtListState.FirstOrDefault(t => t.Name == thought.Name);
            if (existingState != null)
            {
                existingState.State = thought.State == "GAINED" ? "UNKNOWN" : thought.State;
                existingState.TimeLeft = thought.State == "COOKING" ? thought.TimeLeft : 0;
            }
        }

        // Rebuild slot states — fill remaining slots as BUYABLE or LOCKED
        var totalSlots = tcState.ThoughtCabinetViewState.SlotStates.Count;
        while (slotStates.Count < totalSlots)
        {
            slotStates.Add(new SlotState { Item1 = "BUYABLE", Item2 = null });
        }
        tcState.ThoughtCabinetViewState.SlotStates = slotStates;
    }

    [RelayCommand]
    private void SetState(ThoughtDisplayItem thought)
    {
        // Cycle: UNKNOWN → COOKING → FIXED → FORGOTTEN → UNKNOWN
        thought.State = thought.State switch
        {
            "UNKNOWN" => "COOKING",
            "GAINED" => "COOKING",
            "COOKING" => "FIXED",
            "FIXED" => "FORGOTTEN",
            "FORGOTTEN" => "UNKNOWN",
            _ => "UNKNOWN"
        };
    }

    [RelayCommand]
    private void InternalizeAll()
    {
        foreach (var t in Thoughts)
        {
            if (t.State is "COOKING" or "GAINED")
                t.State = "FIXED";
        }
    }

    [RelayCommand]
    private void ResetAll()
    {
        foreach (var t in Thoughts)
            t.State = "UNKNOWN";
    }
}

public partial class ThoughtDisplayItem : ObservableObject
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string BonusWhileProcessing { get; set; } = "";
    public string BonusWhenCompleted { get; set; } = "";
    public string ThoughtType { get; set; } = "";
    public double TimeToInternalize { get; set; }
    public string Requirement { get; set; } = "";
    public bool IsCursed { get; set; }
    public ThoughtDisplayItem()
    {
        State = "UNKNOWN";
    }
    [ObservableProperty] public partial string State { get; set; }
    [ObservableProperty] public partial double TimeLeft { get; set; }

    public string StateEmoji => State switch
    {
        "FIXED" => "\u2705",       // checkmark
        "COOKING" => "\u23F3",     // hourglass
        "FORGOTTEN" => "\u274C",   // X
        "GAINED" => "\u2B50",      // star
        _ => "\u2796"              // minus
    };
}
