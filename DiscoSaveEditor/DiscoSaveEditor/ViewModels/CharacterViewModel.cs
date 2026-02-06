using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.GameData;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Character Stats page.
/// Displays abilities, skills, player resources (XP, level, money, health, morale).
/// </summary>
public partial class CharacterViewModel : ObservableObject
{
    private readonly GameDataService _gameData;

    public ObservableCollection<AbilityDisplayItem> Abilities { get; } = new();
    public ObservableCollection<SkillDisplayItem> Skills { get; } = new();

    // Player resources
    [ObservableProperty] public partial int XpAmount { get; set; }
    [ObservableProperty] public partial int Level { get; set; }
    [ObservableProperty] public partial int SkillPoints { get; set; }
    [ObservableProperty] public partial int Money { get; set; }
    [ObservableProperty] public partial int Health { get; set; }
    [ObservableProperty] public partial int Morale { get; set; }

    // Time
    [ObservableProperty] public partial int Day { get; set; }
    [ObservableProperty] public partial int Hours { get; set; }
    [ObservableProperty] public partial int Minutes { get; set; }

    public CharacterViewModel(GameDataService gameData)
    {
        _gameData = gameData;
    }

    [RelayCommand]
    private void MaxAllSkills()
    {
        foreach (var skill in Skills)
        {
            skill.Value = 20;
        }
    }

    public void LoadFromSave(SaveData save)
    {
        var cs = save.Second.CharacterSheet;
        var pc = save.Second.PlayerCharacter;

        // Player resources
        XpAmount = pc.XpAmount;
        Level = pc.Level;
        SkillPoints = pc.SkillPoints;
        Money = pc.Money;
        Health = pc.HealingPools.Endurance;
        Morale = pc.HealingPools.Volition;

        // Time
        var time = save.Second.SunshineClockTimeHolder.Time;
        Day = time.DayCounter;
        Hours = time.Hours;
        Minutes = time.Minutes;

        // Abilities
        Abilities.Clear();
        foreach (var (key, entry) in cs.Abilities)
        {
            var mapping = _gameData.SkillKeyMap.FindAbilityBySaveKey(key);
            Abilities.Add(new AbilityDisplayItem
            {
                SaveKey = key,
                DisplayName = mapping?.DisplayName ?? key,
                TypeCode = mapping?.SkillType ?? "",
                Value = entry.Value,
                MaximumValue = entry.MaximumValue,
                IsSignature = entry.IsSignature
            });
        }

        // Skills grouped by ability
        Skills.Clear();
        foreach (var (key, entry) in cs.Skills)
        {
            var mapping = _gameData.SkillKeyMap.FindBySaveKey(key);
            var skillDef = _gameData.Skills.Values.FirstOrDefault(s =>
                s.DisplayName.Equals(mapping?.DisplayName, StringComparison.OrdinalIgnoreCase));

            // Gather modifiers
            var modifiers = cs.SkillModifierCauseMap
                .GetValueOrDefault(mapping?.SkillType ?? "", new List<ModifierEntry>());

            Skills.Add(new SkillDisplayItem
            {
                SaveKey = key,
                DisplayName = mapping?.DisplayName ?? key,
                TypeCode = mapping?.SkillType ?? "",
                AbilityType = mapping?.Ability ?? entry.AbilityType,
                Description = skillDef?.Description ?? "",
                Value = entry.Value,
                MaximumValue = entry.MaximumValue,
                CalculatedAbility = entry.CalculatedAbility,
                RankValue = entry.RankValue,
                HasAdvancement = entry.HasAdvancement,
                IsSignature = entry.IsSignature,
                ModifierCount = modifiers.Count(m => m.Type != "CALCULATED_ABILITY")
            });
        }
    }

    public void ApplyToSave(SaveData save)
    {
        var cs = save.Second.CharacterSheet;
        var pc = save.Second.PlayerCharacter;

        pc.XpAmount = XpAmount;
        pc.Level = Level;
        pc.SkillPoints = SkillPoints;
        pc.Money = Money;
        pc.HealingPools.Endurance = Health;
        pc.HealingPools.Volition = Morale;

        // Time
        save.Second.SunshineClockTimeHolder.Time.DayCounter = Day;
        save.Second.SunshineClockTimeHolder.Time.RealDayCounter = Day;
        save.Second.SunshineClockTimeHolder.Time.DayMinutes = Hours * 60 + Minutes;

        // Abilities
        foreach (var item in Abilities)
        {
            if (cs.Abilities.TryGetValue(item.SaveKey, out var entry))
            {
                entry.Value = item.Value;
                entry.MaximumValue = Math.Max(item.Value, item.MaximumValue);
                entry.IsSignature = item.IsSignature;
            }
        }

        // Skills
        foreach (var item in Skills)
        {
            if (cs.Skills.TryGetValue(item.SaveKey, out var entry))
            {
                entry.Value = item.Value;
                entry.MaximumValue = Math.Max(item.Value, item.MaximumValue);
                entry.RankValue = item.RankValue;
                entry.HasAdvancement = item.HasAdvancement;
                entry.IsSignature = item.IsSignature;
            }
        }
    }
}

/// <summary>Display item for an ability (INT/PSY/FYS/MOT)</summary>
public partial class AbilityDisplayItem : ObservableObject
{
    public string SaveKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string TypeCode { get; set; } = "";

    [ObservableProperty] public partial int Value { get; set; }
    [ObservableProperty] public partial int MaximumValue { get; set; }
    [ObservableProperty] public partial bool IsSignature { get; set; }
}

/// <summary>Display item for a skill</summary>
public partial class SkillDisplayItem : ObservableObject
{
    public string SaveKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string TypeCode { get; set; } = "";
    public string AbilityType { get; set; } = "";
    public string Description { get; set; } = "";

    [ObservableProperty] public partial int Value { get; set; }
    [ObservableProperty] public partial int MaximumValue { get; set; }
    [ObservableProperty] public partial int CalculatedAbility { get; set; }
    [ObservableProperty] public partial int RankValue { get; set; }
    [ObservableProperty] public partial bool HasAdvancement { get; set; }
    [ObservableProperty] public partial bool IsSignature { get; set; }
    [ObservableProperty] public partial int ModifierCount { get; set; }
}
