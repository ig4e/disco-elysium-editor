using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

/// <summary>
/// Represents the .2nd.ntwtf.json save file.
/// Contains character sheet, inventory, thoughts, journal, containers, HUD, etc.
/// </summary>
public class SecondFile
{
    [JsonPropertyName("variousItemsHolder")]
    public VariousItemsHolder VariousItemsHolder { get; set; } = new();

    [JsonPropertyName("sunshineClockTimeHolder")]
    public SunshineClockTimeHolder SunshineClockTimeHolder { get; set; } = new();

    [JsonPropertyName("characterSheet")]
    public JsonElement CharacterSheetRaw { get; set; }

    [JsonIgnore]
    public CharacterSheet CharacterSheet { get; set; } = new();

    [JsonPropertyName("playerCharacter")]
    public PlayerCharacter PlayerCharacter { get; set; } = new();

    [JsonPropertyName("hudState")]
    public HudState HudState { get; set; } = new();

    [JsonPropertyName("aquiredJournalTasks")]
    public AcquiredJournalTasks AcquiredJournalTasks { get; set; } = new();

    [JsonPropertyName("failedWhiteChecksHolder")]
    public FailedWhiteChecksHolder FailedWhiteChecksHolder { get; set; } = new();

    [JsonPropertyName("weatherState")]
    public WeatherState WeatherState { get; set; } = new();

    [JsonPropertyName("inventoryState")]
    public InventoryState InventoryState { get; set; } = new();

    [JsonPropertyName("thoughtCabinetState")]
    public ThoughtCabinetState ThoughtCabinetState { get; set; } = new();

    [JsonPropertyName("containerSourceState")]
    public ContainerSourceState ContainerSourceState { get; set; } = new();

    [JsonPropertyName("kubujussState")]
    public JsonElement? KubujussState { get; set; }

    [JsonPropertyName("gameModeState")]
    public GameModeState GameModeState { get; set; } = new();
}

public class VariousItemsHolder
{
    [JsonPropertyName("Obsessions")]
    public List<string> Obsessions { get; set; } = new();

    [JsonPropertyName("DoorStates")]
    public Dictionary<string, bool> DoorStates { get; set; } = new();

    [JsonPropertyName("BuildNumber")]
    public string BuildNumber { get; set; } = "";
}

public class SunshineClockTimeHolder
{
    [JsonPropertyName("time")]
    public GameTimestamp Time { get; set; } = new();

    [JsonPropertyName("timeOverride")]
    public JsonElement? TimeOverride { get; set; }
}

public class GameTimestamp
{
    [JsonPropertyName("dayCounter")] public int DayCounter { get; set; }
    [JsonPropertyName("realDayCounter")] public int RealDayCounter { get; set; }
    [JsonPropertyName("dayMinutes")] public int DayMinutes { get; set; }
    [JsonPropertyName("seconds")] public int Seconds { get; set; }

    /// <summary>Hours component from DayMinutes (0-23)</summary>
    [JsonIgnore] public int Hours => DayMinutes / 60;
    /// <summary>Minutes component from DayMinutes (0-59)</summary>
    [JsonIgnore] public int Minutes => DayMinutes % 60;

    public override string ToString() => $"Day {DayCounter}, {Hours:D2}:{Minutes:D2}:{Seconds:D2}";
}

public class PlayerCharacter
{
    [JsonPropertyName("XpAmount")] public int XpAmount { get; set; }
    [JsonPropertyName("Level")] public int Level { get; set; }
    [JsonPropertyName("SkillPoints")] public int SkillPoints { get; set; }
    [JsonPropertyName("Money")] public int Money { get; set; }
    [JsonPropertyName("StockValue")] public int StockValue { get; set; }
    [JsonPropertyName("NewPointsToSpend")] public bool NewPointsToSpend { get; set; }
    [JsonPropertyName("healingPools")] public HealingPools HealingPools { get; set; } = new();

    /// <summary>Money in Réal (cents to currency)</summary>
    [JsonIgnore] public double MoneyInReal => Money / 100.0;
}

public class HealingPools
{
    [JsonPropertyName("ENDURANCE")] public int Endurance { get; set; }
    [JsonPropertyName("VOLITION")] public int Volition { get; set; }
}

public class HudState
{
    [JsonPropertyName("tequilaPortraitObscured")] public bool TequilaPortraitObscured { get; set; }
    [JsonPropertyName("tequilaPortraitShaved")] public bool TequilaPortraitShaved { get; set; }
    [JsonPropertyName("tequilaPortraitExpressionStopped")] public bool TequilaPortraitExpressionStopped { get; set; }
    [JsonPropertyName("tequilaPortraitFascist")] public bool TequilaPortraitFascist { get; set; }
    [JsonPropertyName("charsheetNotification")] public bool CharsheetNotification { get; set; }
    [JsonPropertyName("inventoryNotification")] public bool InventoryNotification { get; set; }
    [JsonPropertyName("journalNotification")] public bool JournalNotification { get; set; }
    [JsonPropertyName("thcNotification")] public bool ThcNotification { get; set; }
    [JsonPropertyName("invClothesNotification")] public bool InvClothesNotification { get; set; }
    [JsonPropertyName("invPawnablesNotification")] public bool InvPawnablesNotification { get; set; }
    [JsonPropertyName("invReadingNotification")] public bool InvReadingNotification { get; set; }
    [JsonPropertyName("invToolsNotification")] public bool InvToolsNotification { get; set; }
}

public class WeatherState
{
    [JsonPropertyName("weatherPreset")] public int WeatherPreset { get; set; }
}

public class GameModeState
{
    [JsonPropertyName("gameMode")] public string GameMode { get; set; } = "NORMAL";
    [JsonPropertyName("wasSwitched")] public bool WasSwitched { get; set; }
}

public class ContainerSourceState
{
    [JsonPropertyName("itemRegistry")]
    public Dictionary<string, List<ContainerItem>> ItemRegistry { get; set; } = new();
}

public class ContainerItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("probability")] public double Probability { get; set; }
    [JsonPropertyName("value")] public int Value { get; set; }
    [JsonPropertyName("deviation")] public int Deviation { get; set; }
    [JsonPropertyName("calculatedValue")] public int CalculatedValue { get; set; }
    [JsonPropertyName("bonusLoot")] public bool BonusLoot { get; set; }
}
