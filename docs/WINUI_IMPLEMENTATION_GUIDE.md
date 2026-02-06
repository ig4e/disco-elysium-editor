# C# WinUI Save Editor — Implementation Guide

> **Audience**: An AI agent building a Disco Elysium save editor with C# + WinUI 3 (Windows App SDK).  
> **Companion doc**: See `SCHEMA_REFERENCE.md` for complete field-level schemas and sample data.

---

## Table of Contents

1. [Project Setup](#1-project-setup)
2. [Data Loading Architecture](#2-data-loading-architecture)
3. [C# Model Classes](#3-c-model-classes)
4. [Save File I/O](#4-save-file-io)
5. [Editor Feature Map](#5-editor-feature-map)
6. [Binary Lua Database](#6-binary-lua-database)
7. [States.lua Parsing](#7-stateslua-parsing)
8. [Data Relationship Patterns](#8-data-relationship-patterns)
9. [Validation Rules](#9-validation-rules)
10. [File Inventory — What to Copy](#10-file-inventory--what-to-copy)

---

## 1. Project Setup

### Required NuGet Packages

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5+" />
<PackageReference Include="System.Text.Json" Version="8.0+" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.0+" />
<PackageReference Include="CommunityToolkit.WinUI.Controls" Version="8.0+" />
```

### Recommended Project Structure

```
DiscoSaveEditor/
├── DiscoSaveEditor.sln
├── DiscoSaveEditor/
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml
│   ├── Models/
│   │   ├── SaveFile/               # Save file deserialization models
│   │   │   ├── FirstFile.cs
│   │   │   ├── SecondFile.cs
│   │   │   ├── CharacterSheet.cs
│   │   │   ├── SkillEntry.cs
│   │   │   ├── InventoryState.cs
│   │   │   ├── ThoughtCabinetState.cs
│   │   │   ├── JournalTasks.cs
│   │   │   ├── WhiteCheckCache.cs
│   │   │   └── ContainerState.cs
│   │   └── GameData/               # Static game definition models
│   │       ├── Skill.cs
│   │       ├── Item.cs
│   │       ├── Thought.cs
│   │       ├── Variable.cs
│   │       ├── Actor.cs
│   │       └── SkillKeyMap.cs
│   ├── Services/
│   │   ├── SaveFileService.cs      # Read/write save folders
│   │   ├── GameDataService.cs      # Load game asset JSONs
│   │   ├── LuaDatabaseService.cs   # Binary .ntwtf.lua parser
│   │   └── StatesLuaService.cs     # Parse/write states.lua
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── CharacterViewModel.cs
│   │   ├── InventoryViewModel.cs
│   │   ├── ThoughtCabinetViewModel.cs
│   │   └── JournalViewModel.cs
│   ├── Views/
│   │   ├── CharacterPage.xaml
│   │   ├── InventoryPage.xaml
│   │   ├── ThoughtCabinetPage.xaml
│   │   └── JournalPage.xaml
│   └── Assets/
│       └── GameData/               # ← Copy output/game_assets/*.json here
│           ├── actors_skills.json
│           ├── items_inventory.json
│           ├── items_thoughts.json
│           ├── skill_key_map.json
│           ├── variables_tasks.json
│           ├── variables_xp.json
│           ├── variables_reputation.json
│           └── ... (all game asset JSONs)
```

---

## 2. Data Loading Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    GameDataService (singleton)                │
│  Loaded once at startup from embedded Assets/GameData/       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │ Skills   │ │ Items    │ │ Thoughts │ │ SkillKeyMap   │  │
│  │ Dict<str>│ │ Dict<str>│ │ Dict<str>│ │               │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                    │
│  │ TaskVars │ │ XpVars   │ │ RepVars  │                    │
│  │ Dict<str>│ │ Dict<str>│ │ Dict<str>│                    │
│  └──────────┘ └──────────┘ └──────────┘                    │
└─────────────────────────────────────────────────────────────┘
                              ↕ join by name/ID
┌─────────────────────────────────────────────────────────────┐
│                SaveFileService (per-save instance)           │
│  Loaded when user opens a .ntwtf folder                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │ FirstFile│ │SecondFile│ │StatesLua │ │ LuaDatabase   │  │
│  │ .1st JSON│ │ .2nd JSON│ │ text Lua │ │ binary TLV    │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

**Key principle**: Game assets are keyed by `name` (string). Save file entries reference these same `name` values. To display a skill, look up its save key in `SkillKeyMap`, then find the matching entry in `actors_skills.json` for display name and description.

---

## 3. C# Model Classes

### 3.1 Save File — First File

```csharp
public class FirstFile
{
    [JsonPropertyName("areaId")]
    public string AreaId { get; set; } = "";

    [JsonPropertyName("partyState")]
    public PartyState PartyState { get; set; } = new();

    [JsonPropertyName("fowUnrevealersStatusCache")]
    public Dictionary<string, string> FowUnrevealersStatusCache { get; set; } = new();
}

public class PartyState
{
    [JsonPropertyName("isKimInParty")] public bool IsKimInParty { get; set; }
    [JsonPropertyName("isKimLeftOutside")] public bool IsKimLeftOutside { get; set; }
    [JsonPropertyName("isKimAbandoned")] public bool IsKimAbandoned { get; set; }
    [JsonPropertyName("isKimAwayUpToMorning")] public bool IsKimAwayUpToMorning { get; set; }
    [JsonPropertyName("isKimSleepingInHisRoom")] public bool IsKimSleepingInHisRoom { get; set; }
    [JsonPropertyName("isKimSayingGoodMorning")] public bool IsKimSayingGoodMorning { get; set; }
    [JsonPropertyName("isCunoInParty")] public bool IsCunoInParty { get; set; }
    [JsonPropertyName("isCunoLeftOutside")] public bool IsCunoLeftOutside { get; set; }
    [JsonPropertyName("isCunoAbandoned")] public bool IsCunoAbandoned { get; set; }
    [JsonPropertyName("hasHangover")] public bool HasHangover { get; set; }
    [JsonPropertyName("sleepLocation")] public int SleepLocation { get; set; }
    [JsonPropertyName("waitLocation")] public int WaitLocation { get; set; }
    [JsonPropertyName("cunoWaitLocation")] public int CunoWaitLocation { get; set; }
    [JsonPropertyName("timeSinceKimWentSleepingInHisRoom")] public int TimeSinceKimWentSleepingInHisRoom { get; set; }
    [JsonPropertyName("kimLastArrivalLocation")] public int KimLastArrivalLocation { get; set; }
    [JsonPropertyName("cunoLastArrivalLocation")] public int CunoLastArrivalLocation { get; set; }
}
```

### 3.2 Save File — Skill/Ability Entry

```csharp
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
```

### 3.3 Save File — Character Sheet

The character sheet has dynamic keys — the 4 ability names and 24 skill names are object keys, not an array. Use a custom converter or `JsonNode`:

```csharp
public class CharacterSheet
{
    // Abilities — keys: intellect, psyche, fysique, motorics
    public Dictionary<string, SkillEntry> Abilities { get; set; } = new();

    // Skills — keys: logic, encyclopedia, rhetoric, etc.
    public Dictionary<string, SkillEntry> Skills { get; set; } = new();

    // Collections
    public List<string> GainedItems { get; set; } = new();
    public List<string> EquippedItems { get; set; } = new();
    public List<string> GainedThoughts { get; set; } = new();
    public List<string> CookingThoughts { get; set; } = new();
    public List<string> FixedThoughts { get; set; } = new();
    public List<string> ForgottenThoughts { get; set; } = new();
    public string SelectedPanelName { get; set; } = "";

    // Modifier maps
    public Dictionary<string, List<ModifierEntry>> SkillModifierCauseMap { get; set; } = new();
    public Dictionary<string, List<ModifierEntry>> AbilityModifierCauseMap { get; set; } = new();
}
```

> **Deserialization note**: Since ability/skill keys are mixed in with collection fields and modifier maps, you need a custom `JsonConverter<CharacterSheet>`. Use `skill_key_map.json` to know which keys are abilities vs. skills. All other keys are collection fields or modifier maps.

```csharp
// Pseudo-code for custom deserialization
var abilityKeys = new HashSet<string> { "intellect", "psyche", "fysique", "motorics" };
var skillKeys = skillKeyMap.Skills.Select(s => s.SaveKey).ToHashSet();

foreach (var prop in jsonObject.EnumerateObject())
{
    if (abilityKeys.Contains(prop.Name))
        sheet.Abilities[prop.Name] = Deserialize<SkillEntry>(prop.Value);
    else if (skillKeys.Contains(prop.Name))
        sheet.Skills[prop.Name] = Deserialize<SkillEntry>(prop.Value);
    else if (prop.Name == "gainedItems")
        sheet.GainedItems = Deserialize<List<string>>(prop.Value);
    // ... etc
}
```

### 3.4 Save File — Modifier Entry

```csharp
public class ModifierEntry
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";        // CALCULATED_ABILITY, THC, INVENTORY_ITEM, INITIAL_DICE
    [JsonPropertyName("amount")] public int Amount { get; set; }
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = "";
    [JsonPropertyName("skillType")] public string SkillType { get; set; } = "";
    [JsonPropertyName("modifierCause")] public ModifierCause ModifierCause { get; set; } = new();
}

public class ModifierCause
{
    [JsonPropertyName("ModifierKey")] public string ModifierKey { get; set; } = "";           // item name, thought name, or ability type
    [JsonPropertyName("ModifierCauseType")] public string ModifierCauseType { get; set; } = "";  // ABILITY, THOUGHT, INVENTORY_ITEM
}
```

### 3.5 Save File — Inventory State

```csharp
public class InventoryState
{
    [JsonPropertyName("itemListState")] public List<ItemState> ItemListState { get; set; } = new();
    [JsonPropertyName("inventoryViewState")] public InventoryViewState InventoryViewState { get; set; } = new();
    [JsonPropertyName("wearingBodysuit")] public bool WearingBodysuit { get; set; }
}

public class ItemState
{
    [JsonPropertyName("itemName")] public string ItemName { get; set; } = "";
    [JsonPropertyName("isFresh")] public bool IsFresh { get; set; }
    [JsonPropertyName("substanceUses")] public int SubstanceUses { get; set; }
    [JsonPropertyName("substanceTimeLeft")] public int SubstanceTimeLeft { get; set; }
    [JsonPropertyName("StackItems")] public JsonElement? StackItems { get; set; }
}

public class InventoryViewState
{
    [JsonPropertyName("equipment")] public Dictionary<string, string> Equipment { get; set; } = new();  // slot → item name
    [JsonPropertyName("inventory")] public Dictionary<string, List<InventorySlot>> Inventory { get; set; } = new();
    [JsonPropertyName("bullets")] public int Bullets { get; set; }
    [JsonPropertyName("keys")] public List<string> Keys { get; set; } = new();
    [JsonPropertyName("lastSelectedItem")] public string LastSelectedItem { get; set; } = "";
}

public class InventorySlot
{
    [JsonPropertyName("Key")] public int Key { get; set; }
    [JsonPropertyName("Value")] public string Value { get; set; } = "";
}
```

### 3.6 Save File — Thought Cabinet State

```csharp
public class ThoughtCabinetState
{
    [JsonPropertyName("thoughtListState")] public List<ThoughtState> ThoughtListState { get; set; } = new();
    [JsonPropertyName("thoughtCabinetViewState")] public ThoughtCabinetViewState ThoughtCabinetViewState { get; set; } = new();
}

public class ThoughtState
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("isFresh")] public bool IsFresh { get; set; }
    [JsonPropertyName("state")] public string State { get; set; } = "";     // UNKNOWN, COOKING, FIXED, FORGOTTEN
    [JsonPropertyName("timeLeft")] public double TimeLeft { get; set; }
}

public class ThoughtCabinetViewState
{
    [JsonPropertyName("slotStates")] public List<SlotState> SlotStates { get; set; } = new();
    [JsonPropertyName("selectedProjectName")] public string SelectedProjectName { get; set; } = "";
}

public class SlotState
{
    [JsonPropertyName("Item1")] public string Item1 { get; set; } = "";  // FILLED, BUYABLE, LOCKED
    [JsonPropertyName("Item2")] public string? Item2 { get; set; }       // thought name or null
}
```

### 3.7 Save File — Journal Tasks

```csharp
public class AcquiredJournalTasks
{
    [JsonPropertyName("TaskAquisitions")] public Dictionary<string, GameTimestamp> TaskAcquisitions { get; set; } = new();
    [JsonPropertyName("TaskResolutions")] public Dictionary<string, JsonElement> TaskResolutions { get; set; } = new();  // GameTimestamp or empty {}
    [JsonPropertyName("SubtaskAquisitions")] public Dictionary<string, Dictionary<string, GameTimestamp>> SubtaskAcquisitions { get; set; } = new();
    [JsonPropertyName("TaskNewStates")] public Dictionary<string, bool> TaskNewStates { get; set; } = new();
    [JsonPropertyName("ChecksWithNotifications")] public JsonElement ChecksWithNotifications { get; set; }
    [JsonPropertyName("LastActiveTask")] public string LastActiveTask { get; set; } = "";
    [JsonPropertyName("LastDoneTask")] public string LastDoneTask { get; set; } = "";
    [JsonPropertyName("TasksTabNotifyIcon")] public bool TasksTabNotifyIcon { get; set; }
    [JsonPropertyName("ChecksTabNotifyIcon")] public bool ChecksTabNotifyIcon { get; set; }
    [JsonPropertyName("ActiveTasksTabNotifyIcon")] public bool ActiveTasksTabNotifyIcon { get; set; }
    [JsonPropertyName("DoneTasksTabNotifyIcon")] public bool DoneTasksTabNotifyIcon { get; set; }
    [JsonPropertyName("HudNotifyIcon")] public bool HudNotifyIcon { get; set; }
    [JsonPropertyName("wasChurchVisited")] public bool WasChurchVisited { get; set; }
    [JsonPropertyName("wasFishingVillageVisited")] public bool WasFishingVillageVisited { get; set; }
    [JsonPropertyName("wasQuicktravelChurchDiscovered")] public bool WasQuicktravelChurchDiscovered { get; set; }
    [JsonPropertyName("wasQuicktravelFishingVillageDiscovered")] public bool WasQuicktravelFishingVillageDiscovered { get; set; }
}

public class GameTimestamp
{
    [JsonPropertyName("dayCounter")] public int DayCounter { get; set; }
    [JsonPropertyName("realDayCounter")] public int RealDayCounter { get; set; }
    [JsonPropertyName("dayMinutes")] public int DayMinutes { get; set; }    // 0-1439. hours=DayMinutes/60, mins=DayMinutes%60
    [JsonPropertyName("seconds")] public int Seconds { get; set; }
}
```

### 3.8 Save File — White Check Cache

```csharp
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
    [JsonPropertyName("bonus")] public int Bonus { get; set; }              // negative = makes check easier
    [JsonPropertyName("fallbackExplanation")] public string FallbackExplanation { get; set; } = "";
}
```

### 3.9 Game Data Models

```csharp
// actors_skills.json
public class GameSkill
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("long_description")] public string LongDescription { get; set; } = "";
    [JsonPropertyName("attribute_group")] public string AttributeGroup { get; set; } = "";  // INT, PSY, FYS, MOT
    [JsonPropertyName("is_attribute")] public bool IsAttribute { get; set; }
}

// items_inventory.json
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
    [JsonPropertyName("MediumTextValue")] public string MediumTextValue { get; set; } = "";  // item card tooltip
    [JsonPropertyName("is_quest_item")] public bool IsQuestItem { get; set; }
    [JsonPropertyName("autoequip")] public string Autoequip { get; set; } = "";   // "True" or "False" (string!)
    [JsonPropertyName("cursed")] public string Cursed { get; set; } = "";
    [JsonPropertyName("isSubstance")] public string IsSubstance { get; set; } = "";
    [JsonPropertyName("isConsumable")] public string IsConsumable { get; set; } = "";
    [JsonPropertyName("multipleAllowed")] public string MultipleAllowed { get; set; } = "";

    // Helper — some boolean fields come as strings "True"/"False"
    public bool IsCursed => string.Equals(Cursed, "True", StringComparison.OrdinalIgnoreCase);
    public bool IsAutoequip => string.Equals(Autoequip, "True", StringComparison.OrdinalIgnoreCase);
}

// items_thoughts.json
public class GameThought
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("thought_type")] public string ThoughtType { get; set; } = "";     // INT, PSY, MOT, FYS, other
    [JsonPropertyName("bonus_while_processing")] public string BonusWhileProcessing { get; set; } = "";
    [JsonPropertyName("bonus_when_completed")] public string BonusWhenCompleted { get; set; } = "";
    [JsonPropertyName("completion_description")] public string CompletionDescription { get; set; } = "";
    [JsonPropertyName("time_to_internalize")] public double TimeToInternalize { get; set; }  // minutes
    [JsonPropertyName("requirement")] public string Requirement { get; set; } = "";
    [JsonPropertyName("is_cursed")] public bool IsCursed { get; set; }
}

// variables_tasks.json / variables_*.json
public class GameVariable
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("initial_value")] public string InitialValue { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

// variables_xp.json
public class GameVariableXp : GameVariable
{
    [JsonPropertyName("xp_points")] public string XpPoints { get; set; } = "";
}

// skill_key_map.json
public class SkillKeyMap
{
    [JsonPropertyName("abilities")] public List<SkillMapping> Abilities { get; set; } = new();
    [JsonPropertyName("skills")] public List<SkillMapping> Skills { get; set; } = new();
    [JsonPropertyName("equipment_slots")] public List<string> EquipmentSlots { get; set; } = new();
    [JsonPropertyName("inventory_categories")] public List<string> InventoryCategories { get; set; } = new();
    [JsonPropertyName("modifier_cause_types")] public List<string> ModifierCauseTypes { get; set; } = new();
    [JsonPropertyName("thought_states")] public List<string> ThoughtStates { get; set; } = new();
    [JsonPropertyName("healing_pool_types")] public List<string> HealingPoolTypes { get; set; } = new();
}

public class SkillMapping
{
    [JsonPropertyName("save_key")] public string SaveKey { get; set; } = "";           // camelCase: "visualCalculus"
    [JsonPropertyName("skill_type")] public string SkillType { get; set; } = "";       // UPPER_CASE: "VISUAL_CALCULUS"
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";   // Title Case: "Visual Calculus"
    [JsonPropertyName("ability")] public string Ability { get; set; } = "";             // parent: "INT"
}
```

---

## 4. Save File I/O

### Reading a Save

```csharp
public class SaveFileService
{
    public async Task<SaveData> LoadAsync(string saveFolderPath)
    {
        // A save folder is named "*.ntwtf/" and contains 5 files.
        // All files share the same base name as the folder (minus .ntwtf).
        var folderName = Path.GetFileNameWithoutExtension(saveFolderPath); // strip .ntwtf
        var baseName = folderName;

        var firstPath = Path.Combine(saveFolderPath, $"{baseName}.1st.ntwtf.json");
        var secondPath = Path.Combine(saveFolderPath, $"{baseName}.2nd.ntwtf.json");
        var fowPath = Path.Combine(saveFolderPath, $"{baseName}.FOW.json");
        var luaPath = Path.Combine(saveFolderPath, $"{baseName}.ntwtf.lua");
        var statesPath = Path.Combine(saveFolderPath, $"{baseName}.states.lua");

        var first = JsonSerializer.Deserialize<FirstFile>(
            await File.ReadAllTextAsync(firstPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = false });

        var second = await LoadSecondFileAsync(secondPath);  // needs custom deserialization

        return new SaveData { First = first, Second = second, ... };
    }
}
```

### Writing a Save (preserving formatting)

```csharp
public async Task SaveAsync(SaveData data, string saveFolderPath)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    // IMPORTANT: Write the exact same property order as the original.
    // Use JsonNode-based editing (read → modify → write) rather than
    // full deserialize → serialize to avoid reordering fields.
    // The game's JSON parser may be order-sensitive.

    await File.WriteAllTextAsync(firstPath, JsonSerializer.Serialize(data.First, options));
    await File.WriteAllTextAsync(secondPath, SerializeSecondFile(data.Second, options));
}
```

> **Critical**: Use `JsonNode` for round-trip editing when possible. The game may be sensitive to JSON property order, extra whitespace, or float vs. int serialization. Deserialize to `JsonNode`, modify specific properties, and serialize back to preserve structure.

---

## 5. Editor Feature Map

### Tab → Data Source Mapping

| Editor Tab | Save File Section | Game Asset Files |
|-----------|-------------------|------------------|
| **Character Stats** | `.2nd` → `characterSheet` (abilities + skills) | `actors_skills.json`, `skill_key_map.json` |
| **Player Info** | `.2nd` → `playerCharacter` | — |
| **Inventory** | `.2nd` → `inventoryState`, `characterSheet.gainedItems/equippedItems` | `items_inventory.json` |
| **Equipment** | `.2nd` → `inventoryState.inventoryViewState.equipment` | `items_inventory.json`, `skill_key_map.json` (for slots) |
| **Thought Cabinet** | `.2nd` → `thoughtCabinetState`, `characterSheet.*Thoughts` | `items_thoughts.json` |
| **Journal/Quests** | `.2nd` → `aquiredJournalTasks` | `variables_tasks.json` |
| **Skill Checks** | `.2nd` → `failedWhiteChecksHolder` | `skill_key_map.json`, `variables_locations.json` |
| **Party** | `.1st` → `partyState`, `areaId` | `actors_npcs_major.json` (Kim/Cuno info) |
| **Time/Weather** | `.2nd` → `sunshineClockTimeHolder`, `weatherState` | — |
| **Containers** | `.2nd` → `containerSourceState` | `items_inventory.json` |
| **Reputation** | `.ntwtf.lua` → `reputation.*` variables | `variables_reputation.json` |
| **Game Variables** | `.ntwtf.lua` → all variables | `variables_all.json` |
| **HUD State** | `.2nd` → `hudState` | — |
| **Game Mode** | `.2nd` → `gameModeState` | — |
| **World State** | `.states.lua` | — |

### Priority Features (MVP)

1. **Character stats** — Edit ability/skill values, manage skill points
2. **Player resources** — XP, level, money, health, morale
3. **Inventory** — Add/remove items, equip/unequip
4. **Thought cabinet** — Internalize/forget thoughts
5. **Party** — Toggle Kim/Cuno in party
6. **Time** — Set day/time

### Secondary Features

7. **Journal** — Mark quests complete/incomplete
8. **Skill checks** — Reset failed white checks
9. **Reputation** — Adjust political alignment counters
10. **Game variables** — Browse/edit dialogue variables

---

## 6. Binary Lua Database

The `.ntwtf.lua` file uses a binary TLV (Type-Length-Value) format. This is the most complex file to parse.

### Format Specification

```
File = Header + RootTable
Header = 4 bytes (ignored) + String(key)
RootTable = TypeByte(0x54) + 4-byte padding + 4-byte LE count + (Key, Value) × count
Key = String
Value = TypeByte + payload

TypeByte:
  0x53 ('S') → String: 7bitLength + UTF-8 bytes
  0x4E ('N') → Number: 8-byte LE float64
  0x42 ('B') → Boolean: 1 byte (0 or 1)
  0x54 ('T') → Table: 4-byte padding + 4-byte LE count + pairs
```

### 7-Bit Encoded Length (matches .NET BinaryWriter)

```csharp
private int Read7BitEncodedInt(BinaryReader reader)
{
    int result = 0;
    int shift = 0;
    byte b;
    do
    {
        b = reader.ReadByte();
        result |= (b & 0x7F) << shift;
        shift += 7;
    } while ((b & 0x80) != 0);
    return result;
}
```

### C# Implementation Sketch

```csharp
public class LuaDatabaseService
{
    public Dictionary<string, object> Parse(string filePath)
    {
        using var reader = new BinaryReader(File.OpenRead(filePath), Encoding.UTF8);

        // Skip 4-byte header
        reader.ReadBytes(4);

        // Read root key name (usually "Variable")
        var rootKey = ReadString(reader);

        // Read root table
        var rootType = reader.ReadByte();  // should be 0x54
        return ReadTable(reader);
    }

    private Dictionary<string, object> ReadTable(BinaryReader reader)
    {
        reader.ReadBytes(4);  // padding
        int count = reader.ReadInt32();  // LE
        var dict = new Dictionary<string, object>(count);
        for (int i = 0; i < count; i++)
        {
            var key = ReadString(reader);
            var value = ReadValue(reader);
            dict[key] = value;
        }
        return dict;
    }

    private object ReadValue(BinaryReader reader)
    {
        byte type = reader.ReadByte();
        return type switch
        {
            0x53 => ReadString(reader),     // S = String
            0x4E => reader.ReadDouble(),    // N = Number (float64 LE)
            0x42 => reader.ReadByte() != 0, // B = Boolean
            0x54 => ReadTable(reader),      // T = Table (recursive)
            _ => throw new InvalidDataException($"Unknown type byte: 0x{type:X2}")
        };
    }

    private string ReadString(BinaryReader reader)
    {
        int length = Read7BitEncodedInt(reader);
        return Encoding.UTF8.GetString(reader.ReadBytes(length));
    }
}
```

### Writing Back

Reverse the process: write type byte, then payload. For strings, use `Write7BitEncodedInt` for the length. For tables, write padding + count + key-value pairs. **Preserve the exact key order** from the original parse to avoid breaking the game.

### What's in the Lua Database

~12,000 variables. Most important for the editor:
- `reputation.*` — Political alignment (float values like `3.0`)
- `TASK.*` — Quest completion (boolean `true`/`false`)
- `character.*` — Character flags
- Runtime variables (`Conversation_SimX_*`, `Actor`, `Conversant`) — dialogue engine internals, can be ignored

---

## 7. States.lua Parsing

Plain text Lua with two patterns. Parse with regex:

```csharp
public class StatesLuaService
{
    public StatesData Parse(string content)
    {
        var data = new StatesData();

        // AreaState["NAME"]={LocationState=N};
        foreach (Match m in Regex.Matches(content,
            @"AreaState\[""(.+?)""\]\s*=\s*\{LocationState=(\d+)\}"))
        {
            data.AreaStates[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        }

        // ShownOrbs["NAME"]={OrbSeen=N};
        foreach (Match m in Regex.Matches(content,
            @"ShownOrbs\[""(.+?)""\]\s*=\s*\{OrbSeen=(\d+)\}"))
        {
            data.ShownOrbs[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        }

        return data;
    }

    public string Serialize(StatesData data)
    {
        var sb = new StringBuilder();
        foreach (var (key, val) in data.AreaStates)
            sb.AppendLine($"AreaState[\"{key}\"]={{LocationState={val}}};");
        foreach (var (key, val) in data.ShownOrbs)
            sb.AppendLine($"ShownOrbs[\"{key}\"]={{OrbSeen={val}}};");
        return sb.ToString();
    }
}

public class StatesData
{
    public Dictionary<string, int> AreaStates { get; set; } = new();
    public Dictionary<string, int> ShownOrbs { get; set; } = new();
}
```

---

## 8. Data Relationship Patterns

### Show a Skill in the Editor

```csharp
// 1. Get save data for the skill
var skillEntry = save.Second.CharacterSheet.Skills["visualCalculus"];

// 2. Look up display info from skill_key_map.json
var mapping = gameData.SkillKeyMap.Skills.First(s => s.SaveKey == "visualCalculus");
// mapping.DisplayName = "Visual Calculus"
// mapping.SkillType = "VISUAL_CALCULUS"
// mapping.Ability = "INT"

// 3. Get full description from actors_skills.json
var skillDef = gameData.Skills.First(s => s.Name == mapping.DisplayName);
// skillDef.Description, skillDef.LongDescription

// 4. Get modifiers from SkillModifierCauseMap
var modifiers = save.Second.CharacterSheet.SkillModifierCauseMap
    .GetValueOrDefault("VISUAL_CALCULUS", new List<ModifierEntry>());
```

### Show an Inventory Item

```csharp
// 1. Check if player has it
bool hasItem = save.Second.CharacterSheet.GainedItems.Contains("jacket_suede");

// 2. Check if equipped
bool equipped = save.Second.CharacterSheet.EquippedItems.Contains("jacket_suede");

// 3. Get full item definition
var itemDef = gameData.Items.First(i => i.Name == "jacket_suede");
// itemDef.Description = "Looks like someone skinned this blazer..."
// itemDef.MediumTextValue = "+1 Esprit de Corps: Halogen watermarks"

// 4. Get runtime state (freshness, substance uses)
var itemState = save.Second.InventoryState.ItemListState.First(i => i.ItemName == "jacket_suede");
```

### Show a Thought

```csharp
// 1. Get thought state from save
var thoughtState = save.Second.ThoughtCabinetState.ThoughtListState
    .First(t => t.Name == "hobocop");
// thoughtState.State = "FIXED", "COOKING", "UNKNOWN", or "FORGOTTEN"

// 2. Get thought definition
var thoughtDef = gameData.Thoughts.First(t => t.Name == "hobocop");
// thoughtDef.BonusWhileProcessing, thoughtDef.BonusWhenCompleted
// thoughtDef.TimeToInternalize = 180.0 (minutes)
// thoughtDef.Requirement = unlock condition text
```

### Show a Quest

```csharp
// 1. Check save for quest status
bool acquired = save.Second.JournalTasks.TaskAcquisitions.ContainsKey("TASK.sing_karaoke");
var resolution = save.Second.JournalTasks.TaskResolutions["TASK.sing_karaoke"];
bool resolved = resolution.ValueKind != JsonValueKind.Object
    || resolution.EnumerateObject().Any();  // non-empty = resolved

// 2. Get quest description
var taskVar = gameData.TaskVariables.First(v => v.Name == "TASK.sing_karaoke");
// taskVar.Description = "Sing karaoke at the Whirling-in-Rags"
```

---

## 9. Validation Rules

Apply these when the user edits values:

| Field | Rule |
|-------|------|
| `SkillEntry.Value` | Must be ≥ 1 and ≤ 10 (soft cap), but items/thoughts can push above 10 |
| `SkillEntry.MaximumValue` | Must be ≥ `Value` |
| `SkillEntry.RankValue` | = `Value - CalculatedAbility - Σ(modifier amounts)` |
| `PlayerCharacter.Money` | ≥ 0, in cents (100 = 1.00 Réal) |
| `PlayerCharacter.SkillPoints` | ≥ 0 |
| `HealingPools.ENDURANCE` | ≥ 0 (0 = dead, display warning) |
| `HealingPools.VOLITION` | ≥ 0 (0 = dead, display warning) |
| `EquippedItems` | Must be a subset of `GainedItems` |
| `Equipment` slot values | Must be in `EquippedItems` |
| `ThoughtState.State` | Must follow lifecycle: UNKNOWN → COOKING → FIXED/FORGOTTEN |
| `ThoughtState.TimeLeft` | 0 when `State == "FIXED"`, > 0 when `State == "COOKING"` |
| `SlotState.Item1` | Must be FILLED/BUYABLE/LOCKED |
| `SlotState.Item2` | Must be a thought name when Item1 == FILLED, null otherwise |
| `DayMinutes` | 0–1439 |
| `Seconds` | 0–59 |
| `GameMode` | "NORMAL" or "HARDCORE" only |

### When Adding an Item with Bonuses

1. Parse `MediumTextValue` for bonus info (format: `"+1 Esprit de Corps: Halogen watermarks"`)
2. Add item to `gainedItems[]` and optionally `equippedItems[]`
3. Add `itemListState` entry
4. If equipped: add modifier entry to `SkillModifierCauseMap` for each bonus
5. Recalculate skill `value` = `calculatedAbility + rankValue + Σ(active modifier amounts)`

### When Removing an Item

1. Remove from `equippedItems[]` and `gainedItems[]`
2. Remove from `inventoryViewState.equipment` if equipped
3. Remove matching entries from `SkillModifierCauseMap` (where `ModifierCauseType == "INVENTORY_ITEM"` and `ModifierKey == itemName`)
4. Recalculate affected skill values

---

## 10. File Inventory — What to Copy

### INCLUDE in the editor repo (game asset reference data)

All from `output/game_assets/`:

| File | Purpose | Ship with editor? |
|------|---------|-------------------|
| `actors_skills.json` | Skill/attribute names, descriptions | **Yes** |
| `actors_npcs_major.json` | NPC lookup for display | **Yes** |
| `actors_npcs_minor.json` | Minor NPC lookup | Optional (not directly used) |
| `actors_player.json` | Player actor | Optional |
| `actors_voices.json` | Brain voice names | Optional |
| `items_inventory.json` | Item definitions, bonuses, descriptions | **Yes** |
| `items_thoughts.json` | Thought definitions, bonuses, timers | **Yes** |
| `conversations_index.json` | Conversation metadata | Optional (for advanced view) |
| `variables_tasks.json` | Quest descriptions | **Yes** |
| `variables_xp.json` | XP reward values | Optional |
| `variables_reputation.json` | Reputation counter names | **Yes** |
| `variables_character.json` | Character variable names | Optional |
| `variables_stats.json` | Stats variable names | Optional |
| `variables_inventory.json` | Inventory variable names | Optional |
| `variables_locations.json` | Location flag names | Optional (for white check view) |
| `variables_auto.json` | Auto-trigger variable names | Optional |
| `variables_globals.json` | Global variable names | Optional |
| `variables_all.json` | Complete variable reference | **Yes** (for variable browser) |
| `skill_key_map.json` | Key mapping (essential) | **Yes** |
| `save_file_schema.json` | Schema reference | Dev reference only |
| `_manifest.json` | Extraction metadata | Dev reference only |

### EXCLUDE (do not copy)

| File/Directory | Reason |
|----------------|--------|
| `output/game_assets/dialogue_db_typetree.json` | 265 MB raw extraction cache — not needed at runtime |
| `output/*.json` (root-level) | Old extraction format, superseded by `output/game_assets/` |
| `output/_raw_full_database.json` | Raw dialogue database dump |
| `scraper/` | Extraction tooling (Go + Python), not needed for the editor |
| `examples/` | Sample save files for testing — useful for dev but not for shipping |
| `docs/ASSET_EXTRACTION_RESEARCH.md` | Internal research notes |
| `disco_Data/` | Game install data |
| `*.py`, `*.go` (root) | Development scripts |

### INCLUDE as documentation

| File | Purpose |
|------|---------|
| `docs/SCHEMA_REFERENCE.md` | Complete field-level schema for all files |
| `docs/SAVE_EDITOR_GUIDE.md` | How save data maps to editor features |
| `docs/WINUI_IMPLEMENTATION_GUIDE.md` | This file — C# implementation guide |
| `README.md` | Project overview |
