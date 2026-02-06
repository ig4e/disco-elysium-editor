using System.Text.Json;
using System.Text.Json.Nodes;
using DiscoSaveEditor.Models.SaveFile;

namespace DiscoSaveEditor.Services;

/// <summary>
/// Reads and writes save folders (.ntwtf/).
/// Uses JsonNode for round-trip fidelity — preserving property order and formatting.
/// </summary>
public class SaveFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private readonly GameDataService _gameData;

    public SaveFileService(GameDataService gameData)
    {
        _gameData = gameData;
    }

    /// <summary>
    /// Load a save from its .ntwtf folder.
    /// </summary>
    public async Task<SaveData> LoadAsync(string saveFolderPath)
    {
        var folderName = Path.GetFileName(saveFolderPath);
        // Strip the .ntwtf extension to get the base name
        var baseName = folderName.EndsWith(".ntwtf", StringComparison.OrdinalIgnoreCase)
            ? folderName[..^6]
            : folderName;

        var firstPath = Path.Combine(saveFolderPath, $"{baseName}.1st.ntwtf.json");
        var secondPath = Path.Combine(saveFolderPath, $"{baseName}.2nd.ntwtf.json");
        var luaPath = Path.Combine(saveFolderPath, $"{baseName}.ntwtf.lua");
        var statesPath = Path.Combine(saveFolderPath, $"{baseName}.states.lua");

        var saveData = new SaveData
        {
            FolderPath = saveFolderPath,
            BaseName = baseName
        };

        // Load first file (JSON)
        if (File.Exists(firstPath))
        {
            var firstJson = await File.ReadAllTextAsync(firstPath);
            saveData.FirstRaw = JsonNode.Parse(firstJson);
            saveData.First = JsonSerializer.Deserialize<FirstFile>(firstJson, ReadOptions) ?? new();
        }

        // Load second file (JSON) — needs custom character sheet parsing
        if (File.Exists(secondPath))
        {
            var secondJson = await File.ReadAllTextAsync(secondPath);
            saveData.SecondRaw = JsonNode.Parse(secondJson);
            saveData.Second = JsonSerializer.Deserialize<SecondFile>(secondJson, ReadOptions) ?? new();

            // Parse character sheet from raw JSON using skill key map
            if (saveData.Second.CharacterSheetRaw.ValueKind == JsonValueKind.Object)
            {
                saveData.Second.CharacterSheet = ParseCharacterSheet(saveData.Second.CharacterSheetRaw);
            }
        }

        // Load binary lua database
        if (File.Exists(luaPath))
        {
            var luaService = new LuaDatabaseService();
            saveData.LuaDatabase = luaService.Parse(luaPath);
        }

        // Load states.lua
        if (File.Exists(statesPath))
        {
            var statesContent = await File.ReadAllTextAsync(statesPath);
            var statesService = new StatesLuaService();
            saveData.States = statesService.Parse(statesContent);
        }

        return saveData;
    }

    /// <summary>
    /// Save modifications back to disk. Uses JsonNode-based editing for round-trip fidelity.
    /// </summary>
    public async Task SaveAsync(SaveData data)
    {
        var firstPath = Path.Combine(data.FolderPath, $"{data.BaseName}.1st.ntwtf.json");
        var secondPath = Path.Combine(data.FolderPath, $"{data.BaseName}.2nd.ntwtf.json");
        var statesPath = Path.Combine(data.FolderPath, $"{data.BaseName}.states.lua");
        var luaPath = Path.Combine(data.FolderPath, $"{data.BaseName}.ntwtf.lua");

        // Create backup before writing
        await CreateBackupAsync(data.FolderPath);

        // Write first file
        if (data.FirstRaw != null)
        {
            ApplyFirstFileChanges(data.FirstRaw, data.First);
            await File.WriteAllTextAsync(firstPath, data.FirstRaw.ToJsonString(WriteOptions));
        }

        // Write second file
        if (data.SecondRaw != null)
        {
            ApplySecondFileChanges(data.SecondRaw, data);
            await File.WriteAllTextAsync(secondPath, data.SecondRaw.ToJsonString(WriteOptions));
        }

        // Write states.lua
        var statesService = new StatesLuaService();
        await File.WriteAllTextAsync(statesPath, statesService.Serialize(data.States));

        // Write lua database
        var luaService = new LuaDatabaseService();
        luaService.Write(luaPath, data.LuaDatabase);
    }

    private async Task CreateBackupAsync(string folderPath)
    {
        var backupFolder = folderPath + ".backup";
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
            foreach (var file in Directory.GetFiles(folderPath))
            {
                var destFile = Path.Combine(backupFolder, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, overwrite: true));
            }
        }
    }

    /// <summary>
    /// Parse characterSheet from raw JSON, separating abilities/skills from collection fields.
    /// </summary>
    private CharacterSheet ParseCharacterSheet(JsonElement element)
    {
        var sheet = new CharacterSheet();
        var abilityKeys = _gameData.SkillKeyMap.GetAbilityKeys();
        var skillKeys = _gameData.SkillKeyMap.GetSkillKeys();

        foreach (var prop in element.EnumerateObject())
        {
            if (abilityKeys.Contains(prop.Name))
            {
                sheet.Abilities[prop.Name] = JsonSerializer.Deserialize<SkillEntry>(prop.Value.GetRawText(), ReadOptions) ?? new();
            }
            else if (skillKeys.Contains(prop.Name))
            {
                sheet.Skills[prop.Name] = JsonSerializer.Deserialize<SkillEntry>(prop.Value.GetRawText(), ReadOptions) ?? new();
            }
            else
            {
                switch (prop.Name)
                {
                    case "gainedItems":
                        sheet.GainedItems = JsonSerializer.Deserialize<List<string>>(prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "equippedItems":
                        sheet.EquippedItems = JsonSerializer.Deserialize<List<string>>(prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "gainedThoughts":
                        sheet.GainedThoughts = JsonSerializer.Deserialize<List<string>>(prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "cookingThoughts":
                        sheet.CookingThoughts = JsonSerializer.Deserialize<List<string>>(prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "fixedThoughts":
                        sheet.FixedThoughts = JsonSerializer.Deserialize<List<string>>(prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "forgottenThoughts":
                        sheet.ForgottenThoughts = JsonSerializer.Deserialize<List<string>>(prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "selectedPanelName":
                        sheet.SelectedPanelName = prop.Value.GetString() ?? "";
                        break;
                    case "SkillModifierCauseMap":
                        sheet.SkillModifierCauseMap = JsonSerializer.Deserialize<Dictionary<string, List<ModifierEntry>>>(
                            prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                    case "AbilityModifierCauseMap":
                        sheet.AbilityModifierCauseMap = JsonSerializer.Deserialize<Dictionary<string, List<ModifierEntry>>>(
                            prop.Value.GetRawText(), ReadOptions) ?? new();
                        break;
                }
            }
        }

        return sheet;
    }

    /// <summary>Apply model changes back to the raw JsonNode for first file</summary>
    private void ApplyFirstFileChanges(JsonNode raw, FirstFile first)
    {
        var obj = raw.AsObject();
        obj["areaId"] = first.AreaId;

        var partyNode = obj["partyState"]?.AsObject();
        if (partyNode != null)
        {
            partyNode["isKimInParty"] = first.PartyState.IsKimInParty;
            partyNode["isKimLeftOutside"] = first.PartyState.IsKimLeftOutside;
            partyNode["isKimAbandoned"] = first.PartyState.IsKimAbandoned;
            partyNode["isKimAwayUpToMorning"] = first.PartyState.IsKimAwayUpToMorning;
            partyNode["isKimSleepingInHisRoom"] = first.PartyState.IsKimSleepingInHisRoom;
            partyNode["isKimSayingGoodMorning"] = first.PartyState.IsKimSayingGoodMorning;
            partyNode["isCunoInParty"] = first.PartyState.IsCunoInParty;
            partyNode["isCunoLeftOutside"] = first.PartyState.IsCunoLeftOutside;
            partyNode["isCunoAbandoned"] = first.PartyState.IsCunoAbandoned;
            partyNode["hasHangover"] = first.PartyState.HasHangover;
            partyNode["sleepLocation"] = first.PartyState.SleepLocation;
            partyNode["waitLocation"] = first.PartyState.WaitLocation;
            partyNode["cunoWaitLocation"] = first.PartyState.CunoWaitLocation;
            partyNode["timeSinceKimWentSleepingInHisRoom"] = first.PartyState.TimeSinceKimWentSleepingInHisRoom;
            partyNode["kimLastArrivalLocation"] = first.PartyState.KimLastArrivalLocation;
            partyNode["cunoLastArrivalLocation"] = first.PartyState.CunoLastArrivalLocation;
        }

        // Fog of War cache
        obj["fowUnrevealersStatusCache"] = JsonNode.Parse(JsonSerializer.Serialize(first.FowUnrevealersStatusCache));
    }

    /// <summary>Apply model changes back to the raw JsonNode for second file</summary>
    private void ApplySecondFileChanges(JsonNode raw, SaveData data)
    {
        var obj = raw.AsObject();
        var second = data.Second;

        // Player character
        var pcNode = obj["playerCharacter"]?.AsObject();
        if (pcNode != null)
        {
            pcNode["XpAmount"] = second.PlayerCharacter.XpAmount;
            pcNode["Level"] = second.PlayerCharacter.Level;
            pcNode["SkillPoints"] = second.PlayerCharacter.SkillPoints;
            pcNode["Money"] = second.PlayerCharacter.Money;

            var healNode = pcNode["healingPools"]?.AsObject();
            if (healNode != null)
            {
                healNode["ENDURANCE"] = second.PlayerCharacter.HealingPools.Endurance;
                healNode["VOLITION"] = second.PlayerCharacter.HealingPools.Volition;
            }
        }

        // Time
        var timeHolder = obj["sunshineClockTimeHolder"]?.AsObject();
        var timeNode = timeHolder?["time"]?.AsObject();
        if (timeNode != null)
        {
            timeNode["dayCounter"] = second.SunshineClockTimeHolder.Time.DayCounter;
            timeNode["realDayCounter"] = second.SunshineClockTimeHolder.Time.RealDayCounter;
            timeNode["dayMinutes"] = second.SunshineClockTimeHolder.Time.DayMinutes;
            timeNode["seconds"] = second.SunshineClockTimeHolder.Time.Seconds;
        }

        // Character sheet — write skill/ability values back
        var csNode = obj["characterSheet"]?.AsObject();
        if (csNode != null)
        {
            foreach (var (key, entry) in second.CharacterSheet.Abilities)
            {
                var skillNode = csNode[key]?.AsObject();
                if (skillNode != null)
                {
                    skillNode["value"] = entry.Value;
                    skillNode["maximumValue"] = entry.MaximumValue;
                    skillNode["rankValue"] = entry.RankValue;
                    skillNode["isSignature"] = entry.IsSignature;
                }
            }

            foreach (var (key, entry) in second.CharacterSheet.Skills)
            {
                var skillNode = csNode[key]?.AsObject();
                if (skillNode != null)
                {
                    skillNode["value"] = entry.Value;
                    skillNode["maximumValue"] = entry.MaximumValue;
                    skillNode["rankValue"] = entry.RankValue;
                    skillNode["calculatedAbility"] = entry.CalculatedAbility;
                    skillNode["hasAdvancement"] = entry.HasAdvancement;
                }
            }

            // Write collection arrays
            csNode["gainedItems"] = JsonNode.Parse(JsonSerializer.Serialize(second.CharacterSheet.GainedItems));
            csNode["equippedItems"] = JsonNode.Parse(JsonSerializer.Serialize(second.CharacterSheet.EquippedItems));
            csNode["gainedThoughts"] = JsonNode.Parse(JsonSerializer.Serialize(second.CharacterSheet.GainedThoughts));
            csNode["cookingThoughts"] = JsonNode.Parse(JsonSerializer.Serialize(second.CharacterSheet.CookingThoughts));
            csNode["fixedThoughts"] = JsonNode.Parse(JsonSerializer.Serialize(second.CharacterSheet.FixedThoughts));
            csNode["forgottenThoughts"] = JsonNode.Parse(JsonSerializer.Serialize(second.CharacterSheet.ForgottenThoughts));
        }

        // Game mode
        var gmNode = obj["gameModeState"]?.AsObject();
        if (gmNode != null)
        {
            gmNode["gameMode"] = second.GameModeState.GameMode;
            gmNode["wasSwitched"] = second.GameModeState.WasSwitched;
        }

        // HUD state
        var hudNode = obj["hudState"]?.AsObject();
        if (hudNode != null)
        {
            hudNode["tequilaPortraitObscured"] = second.HudState.TequilaPortraitObscured;
            hudNode["tequilaPortraitShaved"] = second.HudState.TequilaPortraitShaved;
            hudNode["tequilaPortraitExpressionStopped"] = second.HudState.TequilaPortraitExpressionStopped;
            hudNode["tequilaPortraitFascist"] = second.HudState.TequilaPortraitFascist;
            hudNode["charsheetNotification"] = second.HudState.CharsheetNotification;
            hudNode["inventoryNotification"] = second.HudState.InventoryNotification;
            hudNode["journalNotification"] = second.HudState.JournalNotification;
            hudNode["thcNotification"] = second.HudState.ThcNotification;
            hudNode["invClothesNotification"] = second.HudState.InvClothesNotification;
            hudNode["invPawnablesNotification"] = second.HudState.InvPawnablesNotification;
            hudNode["invReadingNotification"] = second.HudState.InvReadingNotification;
            hudNode["invToolsNotification"] = second.HudState.InvToolsNotification;
        }

        // Thought cabinet state
        var tcNode = obj["thoughtCabinetState"]?.AsObject();
        if (tcNode != null)
        {
            tcNode["thoughtListState"] = JsonNode.Parse(JsonSerializer.Serialize(second.ThoughtCabinetState.ThoughtListState, WriteOptions));
            tcNode["thoughtCabinetViewState"] = JsonNode.Parse(JsonSerializer.Serialize(second.ThoughtCabinetState.ThoughtCabinetViewState, WriteOptions));
        }

        // Journal tasks
        var jtNode = obj["aquiredJournalTasks"]?.AsObject();
        if (jtNode != null)
        {
            jtNode["TaskAquisitions"] = JsonNode.Parse(JsonSerializer.Serialize(second.AcquiredJournalTasks.TaskAcquisitions, WriteOptions));
            jtNode["TaskResolutions"] = JsonNode.Parse(JsonSerializer.Serialize(second.AcquiredJournalTasks.TaskResolutions, WriteOptions));
            jtNode["TaskNewStates"] = JsonNode.Parse(JsonSerializer.Serialize(second.AcquiredJournalTasks.TaskNewStates, WriteOptions));
            jtNode["wasChurchVisited"] = second.AcquiredJournalTasks.WasChurchVisited;
            jtNode["wasFishingVillageVisited"] = second.AcquiredJournalTasks.WasFishingVillageVisited;
            jtNode["wasQuicktravelChurchDiscovered"] = second.AcquiredJournalTasks.WasQuicktravelChurchDiscovered;
            jtNode["wasQuicktravelFishingVillageDiscovered"] = second.AcquiredJournalTasks.WasQuicktravelFishingVillageDiscovered;
        }

        // Inventory state
        var invNode = obj["inventoryState"]?.AsObject();
        if (invNode != null)
        {
            var ivNode = invNode["inventoryViewState"]?.AsObject();
            if (ivNode != null)
            {
                ivNode["bullets"] = second.InventoryState.InventoryViewState.Bullets;
                ivNode["equipment"] = JsonNode.Parse(JsonSerializer.Serialize(second.InventoryState.InventoryViewState.Equipment, WriteOptions));
                ivNode["keys"] = JsonNode.Parse(JsonSerializer.Serialize(second.InventoryState.InventoryViewState.Keys, WriteOptions));
            }
            invNode["itemListState"] = JsonNode.Parse(JsonSerializer.Serialize(second.InventoryState.ItemListState, WriteOptions));
            invNode["wearingBodysuit"] = second.InventoryState.WearingBodysuit;
        }

        // Weather state
        var weatherNode = obj["weatherState"]?.AsObject();
        if (weatherNode != null)
        {
            weatherNode["weatherPreset"] = second.WeatherState.WeatherPreset;
        }

        // Various items holder (door states, obsessions)
        var vihNode = obj["variousItemsHolder"]?.AsObject();
        if (vihNode != null)
        {
            vihNode["DoorStates"] = JsonNode.Parse(JsonSerializer.Serialize(second.VariousItemsHolder.DoorStates, WriteOptions));
            vihNode["Obsessions"] = JsonNode.Parse(JsonSerializer.Serialize(second.VariousItemsHolder.Obsessions, WriteOptions));
        }

        // Failed white checks
        var fwcNode = obj["failedWhiteChecksHolder"]?.AsObject();
        if (fwcNode != null)
        {
            fwcNode["SeenWhiteCheckCache"] = JsonNode.Parse(JsonSerializer.Serialize(second.FailedWhiteChecksHolder.SeenWhiteCheckCache, WriteOptions));
        }
    }
}
