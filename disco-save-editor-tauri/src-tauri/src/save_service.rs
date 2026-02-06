use std::collections::HashMap;
use std::path::Path;
use std::io::{Read, Write};
use crate::models::*;
use crate::game_data::GameDataService;
use crate::lua_database;
use crate::states_lua;

/// Load a save from its .ntwtf folder or .zip file
pub fn load_save(save_path: &str, game_data: &GameDataService) -> Result<(serde_json::Value, serde_json::Value, CharacterSheet, FirstFile, SecondFile, HashMap<String, LuaValue>, StatesData), String> {
    let path = Path::new(save_path);
    let filename = path.file_name()
        .ok_or("Invalid path")?
        .to_string_lossy();

    let is_zip = filename.ends_with(".zip");

    let base_name = if filename.ends_with(".ntwtf.zip") {
        &filename[..filename.len() - 10]
    } else if filename.ends_with(".ntwtf") {
        &filename[..filename.len() - 6]
    } else {
        &filename
    };

    let (first_content, second_content, lua_content, states_content) = if is_zip {
        let file = std::fs::File::open(path).map_err(|e| format!("Failed to open zip: {}", e))?;
        let mut archive = zip::ZipArchive::new(file).map_err(|e| format!("Invalid zip archive: {}", e))?;

        let read_entry = |name: String, archive: &mut zip::ZipArchive<std::fs::File>| -> Option<Vec<u8>> {
            let mut file = archive.by_name(&name).ok()?;
            let mut content = Vec::new();
            file.read_to_end(&mut content).ok()?;
            Some(content)
        };

        (
            read_entry(format!("{}.1st.ntwtf.json", base_name), &mut archive),
            read_entry(format!("{}.2nd.ntwtf.json", base_name), &mut archive),
            read_entry(format!("{}.ntwtf.lua", base_name), &mut archive),
            read_entry(format!("{}.states.lua", base_name), &mut archive),
        )
    } else {
        let read_file = |p: std::path::PathBuf| -> Option<Vec<u8>> {
            std::fs::read(p).ok()
        };

        (
            read_file(path.join(format!("{}.1st.ntwtf.json", base_name))),
            read_file(path.join(format!("{}.2nd.ntwtf.json", base_name))),
            read_file(path.join(format!("{}.ntwtf.lua", base_name))),
            read_file(path.join(format!("{}.states.lua", base_name))),
        )
    };

    // Parse first file
    let (first_raw, first): (serde_json::Value, FirstFile) = if let Some(content) = first_content {
        let content_str = String::from_utf8_lossy(&content);
        let raw: serde_json::Value = serde_json::from_str(&content_str)
            .map_err(|e| format!("Failed to parse 1st file: {}", e))?;
        let first: FirstFile = serde_json::from_str(&content_str)
            .map_err(|e| format!("Failed to deserialize 1st file: {}", e))?;
        (raw, first)
    } else {
        (serde_json::Value::Null, FirstFile::default())
    };

    // Parse second file
    let (second_raw, second): (serde_json::Value, SecondFile) = if let Some(content) = second_content {
        let content_str = String::from_utf8_lossy(&content);
        let raw: serde_json::Value = serde_json::from_str(&content_str)
            .map_err(|e| format!("Failed to parse 2nd file: {}", e))?;
        let second: SecondFile = serde_json::from_str(&content_str)
            .map_err(|e| format!("Failed to deserialize 2nd file: {}", e))?;
        (raw, second)
    } else {
        (serde_json::Value::Null, SecondFile::default())
    };

    // Parse character sheet from raw JSON
    let character_sheet = parse_character_sheet(&second.character_sheet_raw, game_data);

    // Parse lua database
    let lua_db = if let Some(content) = lua_content {
        lua_database::parse_lua_data(&content)
            .map_err(|e| format!("Failed to parse lua database: {}", e))?
    } else {
        HashMap::new()
    };

    // Parse states
    let states = if let Some(content) = states_content {
        let content_str = String::from_utf8_lossy(&content);
        states_lua::parse_states(&content_str)
    } else {
        StatesData::default()
    };

    Ok((first_raw, second_raw, character_sheet, first, second, lua_db, states))
}

fn parse_character_sheet(raw: &serde_json::Value, game_data: &GameDataService) -> CharacterSheet {
    let mut sheet = CharacterSheet::default();

    if let serde_json::Value::Object(map) = raw {
        let ability_keys = game_data.skill_key_map.ability_keys();
        let skill_keys = game_data.skill_key_map.skill_keys();

        for (key, value) in map {
            if ability_keys.contains(key) {
                if let Ok(entry) = serde_json::from_value::<SkillEntry>(value.clone()) {
                    sheet.abilities.insert(key.clone(), entry);
                }
            } else if skill_keys.contains(key) {
                if let Ok(entry) = serde_json::from_value::<SkillEntry>(value.clone()) {
                    sheet.skills.insert(key.clone(), entry);
                }
            } else {
                match key.as_str() {
                    "gainedItems" => {
                        sheet.gained_items = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "equippedItems" => {
                        sheet.equipped_items = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "gainedThoughts" => {
                        sheet.gained_thoughts = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "cookingThoughts" => {
                        sheet.cooking_thoughts = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "fixedThoughts" => {
                        sheet.fixed_thoughts = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "forgottenThoughts" => {
                        sheet.forgotten_thoughts = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "selectedPanelName" => {
                        sheet.selected_panel_name = value.as_str().unwrap_or("").to_string();
                    }
                    "SkillModifierCauseMap" => {
                        sheet.skill_modifier_cause_map = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    "AbilityModifierCauseMap" => {
                        sheet.ability_modifier_cause_map = serde_json::from_value(value.clone()).unwrap_or_default();
                    }
                    _ => {}
                }
            }
        }
    }

    sheet
}

/// Save modifications back to disk with round-trip fidelity.
pub fn save_to_disk(
    folder_path: &str,
    base_name: &str,
    first_raw: &mut serde_json::Value,
    second_raw: &mut serde_json::Value,
    save_state: &SaveUpdatePayload,
    _character_sheet: &CharacterSheet,
    lua_db: &HashMap<String, LuaValue>,
    states: &StatesData,
) -> Result<(), String> {
    // Create backup
    create_backup(folder_path)?;

    // Apply first file changes
    if let serde_json::Value::Object(obj) = first_raw {
        obj.insert("areaId".to_string(), serde_json::json!(save_state.area_id));

        if let Some(ps) = obj.get_mut("partyState") {
            if let serde_json::Value::Object(party) = ps {
                party.insert("isKimInParty".to_string(), serde_json::json!(save_state.party_state.is_kim_in_party));
                party.insert("isKimLeftOutside".to_string(), serde_json::json!(save_state.party_state.is_kim_left_outside));
                party.insert("isKimAbandoned".to_string(), serde_json::json!(save_state.party_state.is_kim_abandoned));
                party.insert("isKimAwayUpToMorning".to_string(), serde_json::json!(save_state.party_state.is_kim_away_up_to_morning));
                party.insert("isKimSleepingInHisRoom".to_string(), serde_json::json!(save_state.party_state.is_kim_sleeping_in_his_room));
                party.insert("isKimSayingGoodMorning".to_string(), serde_json::json!(save_state.party_state.is_kim_saying_good_morning));
                party.insert("isCunoInParty".to_string(), serde_json::json!(save_state.party_state.is_cuno_in_party));
                party.insert("isCunoLeftOutside".to_string(), serde_json::json!(save_state.party_state.is_cuno_left_outside));
                party.insert("isCunoAbandoned".to_string(), serde_json::json!(save_state.party_state.is_cuno_abandoned));
                party.insert("hasHangover".to_string(), serde_json::json!(save_state.party_state.has_hangover));
                party.insert("sleepLocation".to_string(), serde_json::json!(save_state.party_state.sleep_location));
                party.insert("waitLocation".to_string(), serde_json::json!(save_state.party_state.wait_location));
                party.insert("cunoWaitLocation".to_string(), serde_json::json!(save_state.party_state.cuno_wait_location));
                party.insert("timeSinceKimWentSleepingInHisRoom".to_string(), serde_json::json!(save_state.party_state.time_since_kim_went_sleeping));
                party.insert("kimLastArrivalLocation".to_string(), serde_json::json!(save_state.party_state.kim_last_arrival_location));
                party.insert("cunoLastArrivalLocation".to_string(), serde_json::json!(save_state.party_state.cuno_last_arrival_location));
            }
        }
    }

    // Apply second file changes
    if let serde_json::Value::Object(obj) = second_raw {
        // Player character - check both cases
        let pc_key = if obj.contains_key("playerCharacter") { "playerCharacter" } else { "PlayerCharacter" };
        if let Some(pc) = obj.get_mut(pc_key) {
            if let serde_json::Value::Object(pc_obj) = pc {
                let xp_key = if pc_obj.contains_key("XpAmount") { "XpAmount" } else { "xpAmount" };
                let lvl_key = if pc_obj.contains_key("Level") { "Level" } else { "level" };
                let sp_key = if pc_obj.contains_key("SkillPoints") { "SkillPoints" } else { "skillPoints" };
                let money_key = if pc_obj.contains_key("Money") { "Money" } else { "money" };

                pc_obj.insert(xp_key.to_string(), serde_json::json!(save_state.xp_amount));
                pc_obj.insert(lvl_key.to_string(), serde_json::json!(save_state.level));
                pc_obj.insert(sp_key.to_string(), serde_json::json!(save_state.skill_points));
                pc_obj.insert(money_key.to_string(), serde_json::json!(save_state.money));
                
                if let Some(hp) = pc_obj.get_mut("healingPools") {
                    if let serde_json::Value::Object(hp_obj) = hp {
                        hp_obj.insert("ENDURANCE".to_string(), serde_json::json!(save_state.health));
                        hp_obj.insert("VOLITION".to_string(), serde_json::json!(save_state.morale));
                    }
                }
            }
        }

        // Time
        let time_holder_key = if obj.contains_key("sunshineClockTimeHolder") { "sunshineClockTimeHolder" } else { "SunshineClockTimeHolder" };
        if let Some(th) = obj.get_mut(time_holder_key) {
            if let Some(time) = th.get_mut("time") {
                if let serde_json::Value::Object(time_obj) = time {
                    time_obj.insert("dayCounter".to_string(), serde_json::json!(save_state.day));
                    time_obj.insert("realDayCounter".to_string(), serde_json::json!(save_state.day));
                    time_obj.insert("dayMinutes".to_string(), serde_json::json!(save_state.hours * 60 + save_state.minutes));
                }
            }
        }

        // Character sheet - abilities and skills
        let cs_key = if obj.contains_key("characterSheet") { "characterSheet" } else { "CharacterSheet" };
        if let Some(cs) = obj.get_mut(cs_key) {
            if let serde_json::Value::Object(cs_obj) = cs {
                for ability in &save_state.abilities {
                    if let Some(entry) = cs_obj.get_mut(&ability.save_key) {
                        if let serde_json::Value::Object(e) = entry {
                            e.insert("value".to_string(), serde_json::json!(ability.value));
                            e.insert("maximumValue".to_string(), serde_json::json!(ability.maximum_value.max(ability.value)));
                            e.insert("isSignature".to_string(), serde_json::json!(ability.is_signature));
                        }
                    }
                }

                for skill in &save_state.skills {
                    if let Some(entry) = cs_obj.get_mut(&skill.save_key) {
                        if let serde_json::Value::Object(e) = entry {
                            e.insert("value".to_string(), serde_json::json!(skill.value));
                            e.insert("maximumValue".to_string(), serde_json::json!(skill.maximum_value.max(skill.value)));
                            e.insert("rankValue".to_string(), serde_json::json!(skill.rank_value));
                            e.insert("hasAdvancement".to_string(), serde_json::json!(skill.has_advancement));
                            e.insert("isSignature".to_string(), serde_json::json!(skill.is_signature));
                        }
                    }
                }

                // Items
                let gained: Vec<String> = save_state.owned_items.iter()
                    .filter(|i| i.is_owned).map(|i| i.name.clone()).collect();
                let equipped: Vec<String> = save_state.owned_items.iter()
                    .filter(|i| i.is_equipped).map(|i| i.name.clone()).collect();
                cs_obj.insert("gainedItems".to_string(), serde_json::json!(gained));
                cs_obj.insert("equippedItems".to_string(), serde_json::json!(equipped));

                // Thoughts
                let mut gained_thoughts = vec![];
                let mut cooking_thoughts = vec![];
                let mut fixed_thoughts = vec![];
                let mut forgotten_thoughts = vec![];

                for thought in &save_state.thoughts {
                    match thought.state.as_str() {
                        "Gained" => gained_thoughts.push(thought.name.clone()),
                        "Processing" => {
                            gained_thoughts.push(thought.name.clone());
                            cooking_thoughts.push(thought.name.clone());
                        }
                        "Internalized" => {
                            gained_thoughts.push(thought.name.clone());
                            fixed_thoughts.push(thought.name.clone());
                        }
                        "Forgotten" => {
                            gained_thoughts.push(thought.name.clone());
                            forgotten_thoughts.push(thought.name.clone());
                        }
                        _ => {}
                    }
                }
                cs_obj.insert("gainedThoughts".to_string(), serde_json::json!(gained_thoughts));
                cs_obj.insert("cookingThoughts".to_string(), serde_json::json!(cooking_thoughts));
                cs_obj.insert("fixedThoughts".to_string(), serde_json::json!(fixed_thoughts));
                cs_obj.insert("forgottenThoughts".to_string(), serde_json::json!(forgotten_thoughts));
            }
        }

        // Thought cabinet state
        if let Some(tcs) = obj.get_mut("thoughtCabinetState") {
            if let serde_json::Value::Object(tcs_obj) = tcs {
                let mut list_state = vec![];
                let mut slots = vec![];

                for thought in &save_state.thoughts {
                    if thought.state == "NotAcquired" { continue; }

                    let game_state = match thought.state.as_str() {
                        "Internalized" => "FIXED",
                        "Processing" => "COOKING",
                        "Gained" => "GAINED",
                        "Forgotten" => "FORGOTTEN",
                        _ => "GAINED",
                    };

                    list_state.push(serde_json::json!({
                        "name": thought.name,
                        "isFresh": false,
                        "state": game_state,
                        "timeLeft": thought.time_left
                    }));

                    if thought.state == "Internalized" || thought.state == "Processing" {
                        slots.push(serde_json::json!({
                            "Item1": "FILLED",
                            "Item2": thought.name
                        }));
                    }
                }

                // Fill remaining slots with EMPTY (up to 12 slots is standard in DE)
                while slots.len() < 12 {
                    slots.push(serde_json::json!({
                        "Item1": "EMPTY",
                        "Item2": ""
                    }));
                }

                tcs_obj.insert("thoughtListState".to_string(), serde_json::Value::Array(list_state));
                if let Some(view) = tcs_obj.get_mut("thoughtCabinetViewState") {
                    if let serde_json::Value::Object(v) = view {
                        v.insert("slotStates".to_string(), serde_json::Value::Array(slots));
                    }
                }
            }
        }

        // Game mode
        if let Some(gm) = obj.get_mut("gameModeState") {
            if let serde_json::Value::Object(gm_obj) = gm {
                gm_obj.insert("gameMode".to_string(), serde_json::json!(save_state.game_mode));
            }
        }

        // HUD state
        if let Some(hud) = obj.get_mut("hudState") {
            if let serde_json::Value::Object(h) = hud {
                h.insert("tequilaPortraitObscured".to_string(), serde_json::json!(save_state.hud_state.portrait_obscured));
                h.insert("tequilaPortraitShaved".to_string(), serde_json::json!(save_state.hud_state.portrait_shaved));
                h.insert("tequilaPortraitExpressionStopped".to_string(), serde_json::json!(save_state.hud_state.portrait_expression_stopped));
                h.insert("tequilaPortraitFascist".to_string(), serde_json::json!(save_state.hud_state.portrait_fascist));
                h.insert("charsheetNotification".to_string(), serde_json::json!(save_state.hud_state.charsheet_notification));
                h.insert("inventoryNotification".to_string(), serde_json::json!(save_state.hud_state.inventory_notification));
                h.insert("journalNotification".to_string(), serde_json::json!(save_state.hud_state.journal_notification));
                h.insert("thcNotification".to_string(), serde_json::json!(save_state.hud_state.thc_notification));
                h.insert("invClothesNotification".to_string(), serde_json::json!(save_state.hud_state.inv_clothes_notification));
                h.insert("invPawnablesNotification".to_string(), serde_json::json!(save_state.hud_state.inv_pawnables_notification));
                h.insert("invReadingNotification".to_string(), serde_json::json!(save_state.hud_state.inv_reading_notification));
                h.insert("invToolsNotification".to_string(), serde_json::json!(save_state.hud_state.inv_tools_notification));
            }
        }

        // Weather
        if let Some(ws) = obj.get_mut("weatherState") {
            if let serde_json::Value::Object(w) = ws {
                w.insert("weatherPreset".to_string(), serde_json::json!(save_state.weather_preset));
            }
        }

        // Inventory bullets and equipment
        if let Some(inv) = obj.get_mut("inventoryState") {
            if let serde_json::Value::Object(inv_obj) = inv {
                if let Some(ivs) = inv_obj.get_mut("inventoryViewState") {
                    if let serde_json::Value::Object(ivs_obj) = ivs {
                        ivs_obj.insert("bullets".to_string(), serde_json::json!(save_state.bullets));

                        let mut equipment = serde_json::Map::new();
                        for item in save_state.owned_items.iter().filter(|i| i.is_equipped && !i.equip_slot.is_empty()) {
                            equipment.insert(item.equip_slot.clone(), serde_json::json!(item.name));
                        }
                        ivs_obj.insert("equipment".to_string(), serde_json::Value::Object(equipment));
                    }
                }
            }
        }

        // Door states
        if let Some(vih) = obj.get_mut("variousItemsHolder") {
            if let serde_json::Value::Object(v) = vih {
                v.insert("DoorStates".to_string(), serde_json::json!(save_state.door_states));
            }
        }

        // Journal location flags
        if let Some(jt) = obj.get_mut("aquiredJournalTasks") {
            if let serde_json::Value::Object(j) = jt {
                j.insert("wasChurchVisited".to_string(), serde_json::json!(save_state.location_flags.was_church_visited));
                j.insert("wasFishingVillageVisited".to_string(), serde_json::json!(save_state.location_flags.was_fishing_village_visited));
                j.insert("wasQuicktravelChurchDiscovered".to_string(), serde_json::json!(save_state.location_flags.was_quicktravel_church_discovered));
                j.insert("wasQuicktravelFishingVillageDiscovered".to_string(), serde_json::json!(save_state.location_flags.was_quicktravel_fishing_village_discovered));
            }
        }

        // White checks - remove ones marked for reset
        if let Some(fwc) = obj.get_mut("failedWhiteChecksHolder") {
            if let serde_json::Value::Object(f) = fwc {
                if let Some(wcc) = f.get_mut("WhiteCheckCache") {
                    if let serde_json::Value::Object(w) = wcc {
                        for key in &save_state.reset_check_keys {
                            w.remove(key);
                        }
                    }
                }
                if let Some(swcc) = f.get_mut("SeenWhiteCheckCache") {
                    if let serde_json::Value::Object(s) = swcc {
                        for key in &save_state.reset_seen_check_keys {
                            s.remove(key);
                        }
                    }
                }
            }
        }
    }

    // Write files
    let first_json = serde_json::to_string_pretty(first_raw)
        .map_err(|e| format!("Failed to serialize 1st file: {}", e))?;

    let second_json = serde_json::to_string_pretty(second_raw)
        .map_err(|e| format!("Failed to serialize 2nd file: {}", e))?;

    let lua_bytes = lua_database::serialize_lua_database(lua_db)
        .map_err(|e| format!("Failed to serialize lua database: {}", e))?;

    let states_content = states_lua::serialize_states(states);

    if folder_path.ends_with(".zip") {
        let path = Path::new(folder_path);
        let temp_path = path.with_extension("tmp_zip");
        
        {
            let file = std::fs::File::open(path).map_err(|e| format!("Failed to open original zip: {}", e))?;
            let mut old_archive = zip::ZipArchive::new(file).map_err(|e| format!("Invalid zip: {}", e))?;
            
            let temp_file = std::fs::File::create(&temp_path).map_err(|e| format!("Failed to create temp zip: {}", e))?;
            let mut new_archive = zip::ZipWriter::new(temp_file);
            
            let first_name = format!("{}.1st.ntwtf.json", base_name);
            let second_name = format!("{}.2nd.ntwtf.json", base_name);
            let lua_name = format!("{}.ntwtf.lua", base_name);
            let states_name = format!("{}.states.lua", base_name);
            
            for i in 0..old_archive.len() {
                let mut entry = old_archive.by_index(i).map_err(|e| format!("Failed to read zip entry: {}", e))?;
                let name = entry.name().to_string();
                
                new_archive.start_file::<_, ()>(name.clone(), zip::write::FileOptions::default())
                    .map_err(|e| format!("Failed to start zip file: {}", e))?;
                
                if name == first_name {
                    new_archive.write_all(first_json.as_bytes()).map_err(|e| format!("Failed to write 1st file to zip: {}", e))?;
                } else if name == second_name {
                    new_archive.write_all(second_json.as_bytes()).map_err(|e| format!("Failed to write 2nd file to zip: {}", e))?;
                } else if name == lua_name {
                    new_archive.write_all(&lua_bytes).map_err(|e| format!("Failed to write lua to zip: {}", e))?;
                } else if name == states_name {
                    new_archive.write_all(states_content.as_bytes()).map_err(|e| format!("Failed to write states to zip: {}", e))?;
                } else {
                    let mut content = Vec::new();
                    entry.read_to_end(&mut content).map_err(|e| format!("Failed to read zip entry content: {}", e))?;
                    new_archive.write_all(&content).map_err(|e| format!("Failed to copy zip entry: {}", e))?;
                }
            }
            new_archive.finish().map_err(|e| format!("Failed to finish zip: {}", e))?;
        }
        
        std::fs::rename(temp_path, path).map_err(|e| format!("Failed to rename temp zip: {}", e))?;
    } else {
        let folder = Path::new(folder_path);
        let first_path = folder.join(format!("{}.1st.ntwtf.json", base_name));
        let second_path = folder.join(format!("{}.2nd.ntwtf.json", base_name));
        let lua_path = folder.join(format!("{}.ntwtf.lua", base_name));
        let states_path = folder.join(format!("{}.states.lua", base_name));

        std::fs::write(&first_path, &first_json)
            .map_err(|e| format!("Failed to write 1st file: {}", e))?;

        std::fs::write(&second_path, &second_json)
            .map_err(|e| format!("Failed to write 2nd file: {}", e))?;

        std::fs::write(&lua_path, &lua_bytes)
            .map_err(|e| format!("Failed to write lua database: {}", e))?;

        std::fs::write(&states_path, &states_content)
            .map_err(|e| format!("Failed to write states file: {}", e))?;
    }

    Ok(())
}

fn create_backup(save_path: &str) -> Result<(), String> {
    let path = Path::new(save_path);
    let backup_path = format!("{}.backup", save_path);
    
    if Path::new(&backup_path).exists() {
        // Rotate backups
        let backup2 = format!("{}.backup.2", save_path);
        if Path::new(&backup2).exists() {
            if Path::new(&backup2).is_dir() {
                let _ = std::fs::remove_dir_all(&backup2);
            } else {
                let _ = std::fs::remove_file(&backup2);
            }
        }
        let _ = std::fs::rename(&backup_path, &backup2);
    }

    if path.is_dir() {
        std::fs::create_dir_all(&backup_path)
            .map_err(|e| format!("Failed to create backup dir: {}", e))?;

        if let Ok(entries) = std::fs::read_dir(path) {
            for entry in entries.flatten() {
                if entry.path().is_file() {
                    let dest = Path::new(&backup_path).join(entry.file_name());
                    let _ = std::fs::copy(entry.path(), dest);
                }
            }
        }
    } else {
        std::fs::copy(path, &backup_path)
            .map_err(|e| format!("Failed to copy backup file: {}", e))?;
    }

    Ok(())
}

/// Payload sent from frontend when saving
#[derive(Debug, Clone, serde::Serialize, serde::Deserialize)]
pub struct SaveUpdatePayload {
    pub folder_path: String,
    pub base_name: String,
    // Character
    pub xp_amount: i64,
    pub level: i64,
    pub skill_points: i64,
    pub money: i64,
    pub health: i64,
    pub morale: i64,
    pub day: i64,
    pub hours: i64,
    pub minutes: i64,
    pub abilities: Vec<AbilityDisplay>,
    pub skills: Vec<SkillDisplay>,
    // Inventory
    pub owned_items: Vec<InventoryItemDisplay>,
    pub bullets: i64,
    // Thoughts
    pub thoughts: Vec<ThoughtDisplay>,
    // Party
    pub area_id: String,
    pub party_state: PartyState,
    pub hud_state: HudStateDisplay,
    pub game_mode: String,
    pub location_flags: LocationFlags,
    // World
    pub weather_preset: i64,
    pub reputation: ReputationDisplay,
    pub lua_edits: HashMap<String, String>,
    // White checks to reset
    pub reset_check_keys: Vec<String>,
    pub reset_seen_check_keys: Vec<String>,
    // States
    pub door_states: HashMap<String, bool>,
    pub area_states: HashMap<String, i64>,
    pub shown_orbs: HashMap<String, i64>,
}
