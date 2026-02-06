use std::collections::HashMap;
use std::sync::Mutex;
use tauri::{State, Manager};
use crate::models::*;
use crate::game_data::GameDataService;
use crate::save_service;
use crate::lua_database;

/// Application state shared across commands
pub struct AppState {
    pub game_data: Mutex<GameDataService>,
    pub first_raw: Mutex<Option<serde_json::Value>>,
    pub second_raw: Mutex<Option<serde_json::Value>>,
    pub character_sheet: Mutex<Option<CharacterSheet>>,
    pub first_file: Mutex<Option<FirstFile>>,
    pub second_file: Mutex<Option<SecondFile>>,
    pub lua_database: Mutex<HashMap<String, LuaValue>>,
    pub states: Mutex<Option<StatesData>>,
    pub current_folder: Mutex<Option<String>>,
    pub current_base_name: Mutex<Option<String>>,
}

impl Default for AppState {
    fn default() -> Self {
        Self {
            game_data: Mutex::new(GameDataService::default()),
            first_raw: Mutex::new(None),
            second_raw: Mutex::new(None),
            character_sheet: Mutex::new(None),
            first_file: Mutex::new(None),
            second_file: Mutex::new(None),
            lua_database: Mutex::new(HashMap::new()),
            states: Mutex::new(None),
            current_folder: Mutex::new(None),
            current_base_name: Mutex::new(None),
        }
    }
}

#[tauri::command]
pub fn discover_saves() -> Result<Vec<SaveSummary>, String> {
    let user_dir = dirs::home_dir().ok_or("Cannot find home directory")?;
    let possible_paths = vec![
        user_dir.join("AppData/LocalLow/ZAUM Studio/Disco Elysium/SaveGames"),
        user_dir.join("AppData/LocalLow/ZA-UM/Disco Elysium/SaveGames"),
        user_dir.join("AppData/LocalLow/ZA-UM/Disco Elysium - The Final Cut/SaveGames"),
        user_dir.join("AppData/LocalLow/ZAUM Studio/Disco Elysium - The Final Cut/SaveGames"),
    ];

    let mut saves = Vec::new();

    for path in possible_paths {
        if path.exists() {
            if let Ok(entries) = std::fs::read_dir(&path) {
                for entry in entries.flatten() {
                    let entry_path = entry.path();
                    let name = entry_path.file_name()
                        .unwrap_or_default().to_string_lossy().to_string();
                    
                    let is_save = if entry_path.is_dir() {
                        name.ends_with(".ntwtf")
                    } else {
                        name.ends_with(".ntwtf.zip")
                    };

                    if is_save {
                        let metadata = std::fs::metadata(&entry_path).ok();
                        let last_modified = metadata
                            .and_then(|m| m.modified().ok())
                            .map(|t| format_system_time(t))
                            .unwrap_or_default();

                        let display_name = name
                            .replace(".ntwtf.zip", "")
                            .replace(".ntwtf", "");

                        saves.push(SaveSummary {
                            name: display_name,
                            path: entry_path.to_string_lossy().to_string(),
                            last_modified,
                        });
                    }
                }
            }
        }
    }

    // Sort by last modified descending
    saves.sort_by(|a, b| b.last_modified.cmp(&a.last_modified));
    Ok(saves)
}

fn format_system_time(time: std::time::SystemTime) -> String {
    let duration = time.duration_since(std::time::UNIX_EPOCH).unwrap_or_default();
    let secs = duration.as_secs();
    // Simple ISO-ish format
    let days = secs / 86400;
    let remaining = secs % 86400;
    let hours = remaining / 3600;
    let mins = (remaining % 3600) / 60;
    format!("{}-{:02}:{:02}", days, hours, mins)
}

#[tauri::command]
pub async fn pick_save_file(app: tauri::AppHandle) -> Result<Option<String>, String> {
    use tauri_plugin_dialog::DialogExt;
    
    let (tx, rx) = tokio::sync::oneshot::channel();
    app.dialog().file()
        .add_filter("Disco Elysium Save", &["zip", "ntwtf"])
        .pick_file(move |file_path| {
            let _ = tx.send(file_path.map(|p| p.to_string()));
        });
    
    rx.await.map_err(|e| e.to_string())
}

#[tauri::command]
pub fn load_save(folder_path: String, state: State<AppState>, app: tauri::AppHandle) -> Result<FullSaveState, String> {
    // Load game data if not loaded
    {
        let mut gd = state.game_data.lock().map_err(|e| e.to_string())?;
        if !gd.is_loaded {
            let resource_path = app.path()
                .resource_dir()
                .map_err(|e| format!("Failed to get resource dir: {}", e))?;
            let game_data_path = resource_path.join("game_data");
            gd.load(game_data_path.to_str().unwrap())?;
        }
    }

    let gd = state.game_data.lock().map_err(|e| e.to_string())?;

    let (first_raw, second_raw, character_sheet, first, second, lua_db, states_data) =
        save_service::load_save(&folder_path, &gd)?;

    let folder_name = std::path::Path::new(&folder_path)
        .file_name().unwrap_or_default().to_string_lossy();
    let base_name = if folder_name.ends_with(".ntwtf.zip") {
        folder_name[..folder_name.len() - 10].to_string()
    } else if folder_name.ends_with(".zip") {
        folder_name[..folder_name.len() - 4].to_string()
    } else if folder_name.ends_with(".ntwtf") {
        folder_name[..folder_name.len() - 6].to_string()
    } else {
        folder_name.to_string()
    };

    // Build the full state to send to frontend
    let full_state = build_full_state(
        &folder_path, &base_name, &first, &second, &character_sheet, &lua_db, &states_data, &gd
    );

    // Store in app state for later saving
    *state.first_raw.lock().map_err(|e| e.to_string())? = Some(first_raw);
    *state.second_raw.lock().map_err(|e| e.to_string())? = Some(second_raw);
    *state.character_sheet.lock().map_err(|e| e.to_string())? = Some(character_sheet);
    *state.first_file.lock().map_err(|e| e.to_string())? = Some(first);
    *state.second_file.lock().map_err(|e| e.to_string())? = Some(second);
    *state.lua_database.lock().map_err(|e| e.to_string())? = lua_db;
    *state.states.lock().map_err(|e| e.to_string())? = Some(states_data);
    *state.current_folder.lock().map_err(|e| e.to_string())? = Some(folder_path);
    *state.current_base_name.lock().map_err(|e| e.to_string())? = Some(base_name);

    Ok(full_state)
}

#[tauri::command]
pub fn save_changes(payload: save_service::SaveUpdatePayload, state: State<AppState>) -> Result<(), String> {
    let mut first_raw = state.first_raw.lock().map_err(|e| e.to_string())?;
    let mut second_raw = state.second_raw.lock().map_err(|e| e.to_string())?;
    let character_sheet = state.character_sheet.lock().map_err(|e| e.to_string())?;
    let mut lua_db = state.lua_database.lock().map_err(|e| e.to_string())?;

    let folder_path = payload.folder_path.clone();
    let base_name = payload.base_name.clone();

    // Apply lua edits
    for (key, value) in &payload.lua_edits {
        // Try to preserve the original type
        let flat = lua_database::flatten_lua(&lua_db, "");
        if let Some(original) = flat.get(key) {
            let typed_value = match original {
                LuaValue::Number(_) => {
                    LuaValue::Number(value.parse::<f64>().unwrap_or(0.0))
                }
                LuaValue::Boolean(_) => {
                    LuaValue::Boolean(value.parse::<bool>().unwrap_or(false))
                }
                _ => LuaValue::String(value.clone()),
            };
            lua_database::set_lua_value(&mut lua_db, key, typed_value);
        }
    }

    // Apply reputation to lua db
    lua_database::set_lua_value(&mut lua_db, "reputation.communist", LuaValue::Number(payload.reputation.communist));
    lua_database::set_lua_value(&mut lua_db, "reputation.ultraliberal", LuaValue::Number(payload.reputation.ultraliberal));
    lua_database::set_lua_value(&mut lua_db, "reputation.moralist", LuaValue::Number(payload.reputation.moralist));
    lua_database::set_lua_value(&mut lua_db, "reputation.revacholian_nationhood", LuaValue::Number(payload.reputation.nationalist));
    lua_database::set_lua_value(&mut lua_db, "reputation.kim", LuaValue::Number(payload.reputation.kim));

    let states = StatesData {
        area_states: payload.area_states.clone(),
        shown_orbs: payload.shown_orbs.clone(),
    };

    let cs = character_sheet.as_ref().ok_or("No character sheet loaded")?;

    save_service::save_to_disk(
        &folder_path,
        &base_name,
        first_raw.as_mut().ok_or("No first file loaded")?,
        second_raw.as_mut().ok_or("No second file loaded")?,
        &payload,
        cs,
        &lua_db,
        &states,
    )?;

    Ok(())
}

#[tauri::command]
pub fn get_lua_variables(query: String, limit: usize, state: State<AppState>) -> Result<Vec<LuaVariableDisplay>, String> {
    let lua_db = state.lua_database.lock().map_err(|e| e.to_string())?;
    let gd = state.game_data.lock().map_err(|e| e.to_string())?;

    let flat = lua_database::flatten_lua(&lua_db, "");
    let query_lower = query.to_lowercase();

    let mut vars: Vec<LuaVariableDisplay> = flat.iter()
        .filter(|(k, _)| query.is_empty() || k.to_lowercase().contains(&query_lower))
        .take(limit)
        .map(|(k, v)| {
            let desc = gd.all_variables.get(k)
                .map(|var| var.description.clone())
                .unwrap_or_default();
            LuaVariableDisplay {
                key: k.clone(),
                value: v.to_display_string(),
                var_type: v.type_name().to_string(),
                description: desc,
            }
        })
        .collect();

    vars.sort_by(|a, b| a.key.cmp(&b.key));
    Ok(vars)
}

#[tauri::command]
pub fn get_catalog_items(state: State<AppState>) -> Result<Vec<CatalogItem>, String> {
    let gd = state.game_data.lock().map_err(|e| e.to_string())?;
    Ok(gd.get_all_catalog_items())
}

fn build_full_state(
    folder_path: &str,
    base_name: &str,
    first: &FirstFile,
    second: &SecondFile,
    cs: &CharacterSheet,
    lua_db: &HashMap<String, LuaValue>,
    states: &StatesData,
    gd: &GameDataService,
) -> FullSaveState {
    let pc = &second.player_character;
    let time = &second.sunshine_clock_time_holder.time;

    // Build abilities
    let abilities: Vec<AbilityDisplay> = cs.abilities.iter().map(|(key, entry)| {
        let mapping = gd.skill_key_map.find_ability_by_save_key(key);
        AbilityDisplay {
            save_key: key.clone(),
            display_name: mapping.map(|m| m.display_name.clone()).unwrap_or_else(|| key.clone()),
            type_code: mapping.map(|m| m.skill_type.clone()).unwrap_or_default(),
            value: entry.value,
            maximum_value: entry.maximum_value,
            is_signature: entry.is_signature,
        }
    }).collect();

    // Build skills
    let skills: Vec<SkillDisplay> = cs.skills.iter().map(|(key, entry)| {
        let mapping = gd.skill_key_map.find_by_save_key(key);
        let skill_def = gd.skills.values().find(|s| {
            mapping.map(|m| s.display_name.eq_ignore_ascii_case(&m.display_name)).unwrap_or(false)
        });
        let modifier_count = mapping
            .and_then(|m| cs.skill_modifier_cause_map.get(&m.skill_type))
            .map(|mods| mods.iter().filter(|m| m.modifier_type != "CALCULATED_ABILITY").count())
            .unwrap_or(0);

        SkillDisplay {
            save_key: key.clone(),
            display_name: mapping.map(|m| m.display_name.clone()).unwrap_or_else(|| key.clone()),
            type_code: mapping.map(|m| m.skill_type.clone()).unwrap_or_default(),
            ability_type: mapping.map(|m| m.ability.clone()).unwrap_or_else(|| entry.ability_type.clone()),
            description: skill_def.map(|s| s.description.clone()).unwrap_or_default(),
            value: entry.value,
            maximum_value: entry.maximum_value,
            calculated_ability: entry.calculated_ability,
            rank_value: entry.rank_value,
            has_advancement: entry.has_advancement,
            is_signature: entry.is_signature,
            modifier_count,
        }
    }).collect();

    // Build owned items
    let owned_items: Vec<InventoryItemDisplay> = cs.gained_items.iter().map(|item_name| {
        let game_def = gd.items.get(item_name);
        let item_state = second.inventory_state.item_list_state.iter()
            .find(|i| i.item_name == *item_name);
        let is_equipped = cs.equipped_items.contains(item_name);
        let slot = second.inventory_state.inventory_view_state.equipment.iter()
            .find(|(_, v)| *v == item_name)
            .map(|(k, _)| k.clone())
            .unwrap_or_default();

        InventoryItemDisplay {
            name: item_name.clone(),
            display_name: game_def.map(|d| d.display_name.clone()).unwrap_or_else(|| item_name.clone()),
            description: game_def.map(|d| d.description.clone()).unwrap_or_default(),
            bonus: game_def.map(|d| d.medium_text_value.clone()).unwrap_or_default(),
            is_owned: true,
            is_equipped,
            equip_slot: slot,
            is_quest_item: game_def.map(|d| d.is_quest_item).unwrap_or(false),
            is_cursed: game_def.map(|d| d.is_cursed()).unwrap_or(false),
            is_substance: game_def.map(|d| d.is_substance_item()).unwrap_or(false),
            substance_uses: item_state.map(|s| s.substance_uses).unwrap_or(0),
        }
    }).collect();

    // Build thoughts
    let thoughts: Vec<ThoughtDisplay> = gd.thoughts.values().map(|thought_def| {
        let state = second.thought_cabinet_state.thought_list_state.iter()
            .find(|t| t.name == thought_def.name);

        let current_state = if cs.fixed_thoughts.contains(&thought_def.name) {
            "Internalized"
        } else if cs.cooking_thoughts.contains(&thought_def.name) {
            "Processing"
        } else if cs.forgotten_thoughts.contains(&thought_def.name) {
            "Forgotten"
        } else if cs.gained_thoughts.contains(&thought_def.name) {
            "Gained"
        } else {
            "NotAcquired"
        };

        ThoughtDisplay {
            name: thought_def.name.clone(),
            display_name: thought_def.display_name.clone(),
            description: thought_def.description.clone(),
            bonus_while_processing: thought_def.bonus_while_processing.clone(),
            bonus_when_completed: thought_def.bonus_when_completed.clone(),
            completion_description: thought_def.completion_description.clone(),
            thought_type: thought_def.thought_type.clone(),
            time_to_internalize: thought_def.time_to_internalize,
            requirement: thought_def.requirement.clone(),
            is_cursed: thought_def.is_cursed_thought(),
            state: current_state.to_string(),
            time_left: state.map(|s| s.time_left).unwrap_or(0.0),
        }
    }).collect();

    // Build tasks
    let tasks: Vec<TaskDisplay> = second.acquired_journal_tasks.task_acquisitions.iter().map(|(task_name, acquisition)| {
        let description = gd.get_task_description(task_name);
        let resolution = second.acquired_journal_tasks.task_resolutions.get(task_name);
        let is_resolved = resolution.map(|r| {
            r.is_object() && r.as_object().map(|o| !o.is_empty()).unwrap_or(false)
        }).unwrap_or(false);
        let is_new = second.acquired_journal_tasks.task_new_states
            .get(task_name).copied().unwrap_or(true) == false;

        let subtasks = second.acquired_journal_tasks.subtask_acquisitions
            .get(task_name)
            .map(|subs| subs.keys().map(|k| gd.get_task_description(k)).collect())
            .unwrap_or_default();

        TaskDisplay {
            task_name: task_name.clone(),
            description,
            acquired_time: format!("Day {}, {:02}:{:02}",
                acquisition.day_counter, acquisition.hours(), acquisition.minutes()),
            is_resolved,
            is_new,
            subtasks,
        }
    }).collect();

    // Build white checks
    let failed_checks: Vec<WhiteCheckDisplay> = second.failed_white_checks_holder
        .white_check_cache.iter().filter_map(|(key, element)| {
            serde_json::from_value::<WhiteCheck>(element.clone()).ok().map(|check| {
                WhiteCheckDisplay {
                    key: key.clone(),
                    flag_name: check.flag_name,
                    skill_type: check.skill_type.clone(),
                    skill_display_name: gd.get_skill_display_name_by_type(&check.skill_type),
                    difficulty: check.difficulty,
                    last_skill_value: check.last_skill_value,
                    last_target_value: check.last_target_value,
                    check_precondition: check.check_precondition,
                    is_seen_only: false,
                }
            })
        }).collect();

    let seen_checks: Vec<WhiteCheckDisplay> = second.failed_white_checks_holder
        .seen_white_check_cache.iter().map(|(key, check)| {
            WhiteCheckDisplay {
                key: key.clone(),
                flag_name: check.flag_name.clone(),
                skill_type: check.skill_type.clone(),
                skill_display_name: gd.get_skill_display_name_by_type(&check.skill_type),
                difficulty: check.difficulty,
                last_skill_value: check.last_skill_value,
                last_target_value: check.last_target_value,
                check_precondition: check.check_precondition.clone(),
                is_seen_only: check.is_only_seen,
            }
        }).collect();

    // Build containers
    let containers: Vec<ContainerDisplay> = second.container_source_state.item_registry.iter()
        .map(|(container_id, items)| {
            let container_items: Vec<ContainerItemDisplay> = items.iter().map(|item| {
                ContainerItemDisplay {
                    name: item.name.clone(),
                    probability: item.probability,
                    value: item.value,
                    deviation: item.deviation,
                    calculated_value: item.calculated_value,
                    bonus_loot: item.bonus_loot,
                }
            }).collect();

            ContainerDisplay {
                container_id: container_id.clone(),
                item_count: items.len(),
                total_value: items.iter().map(|i| i.calculated_value).sum(),
                items: container_items,
            }
        }).collect();

    // Reputation from lua db
    let flat = lua_database::flatten_lua(lua_db, "");
    let get_rep = |key: &str| -> f64 {
        flat.get(key).and_then(|v| v.as_f64()).unwrap_or(0.0)
    };

    FullSaveState {
        folder_path: folder_path.to_string(),
        base_name: base_name.to_string(),
        xp_amount: pc.xp_amount,
        level: pc.level,
        skill_points: pc.skill_points,
        money: pc.money,
        health: pc.healing_pools.endurance,
        morale: pc.healing_pools.volition,
        day: time.day_counter,
        hours: time.hours(),
        minutes: time.minutes(),
        abilities,
        skills,
        owned_items,
        bullets: second.inventory_state.inventory_view_state.bullets,
        thoughts,
        tasks,
        area_id: first.area_id.clone(),
        party_state: first.party_state.clone(),
        hud_state: HudStateDisplay {
            portrait_obscured: second.hud_state.tequila_portrait_obscured,
            portrait_shaved: second.hud_state.tequila_portrait_shaved,
            portrait_expression_stopped: second.hud_state.tequila_portrait_expression_stopped,
            portrait_fascist: second.hud_state.tequila_portrait_fascist,
            charsheet_notification: second.hud_state.charsheet_notification,
            inventory_notification: second.hud_state.inventory_notification,
            journal_notification: second.hud_state.journal_notification,
            thc_notification: second.hud_state.thc_notification,
            inv_clothes_notification: second.hud_state.inv_clothes_notification,
            inv_pawnables_notification: second.hud_state.inv_pawnables_notification,
            inv_reading_notification: second.hud_state.inv_reading_notification,
            inv_tools_notification: second.hud_state.inv_tools_notification,
        },
        game_mode: second.game_mode_state.game_mode.clone(),
        location_flags: LocationFlags {
            was_church_visited: second.acquired_journal_tasks.was_church_visited,
            was_fishing_village_visited: second.acquired_journal_tasks.was_fishing_village_visited,
            was_quicktravel_church_discovered: second.acquired_journal_tasks.was_quicktravel_church_discovered,
            was_quicktravel_fishing_village_discovered: second.acquired_journal_tasks.was_quicktravel_fishing_village_discovered,
        },
        weather_preset: second.weather_state.weather_preset,
        reputation: ReputationDisplay {
            communist: get_rep("reputation.communist"),
            ultraliberal: get_rep("reputation.ultraliberal"),
            moralist: get_rep("reputation.moralist"),
            nationalist: get_rep("reputation.revacholian_nationhood"),
            kim: get_rep("reputation.kim"),
        },
        lua_variable_count: flat.len(),
        failed_checks,
        seen_checks,
        containers,
        door_states: second.various_items_holder.door_states.clone(),
        area_states: states.area_states.clone(),
        shown_orbs: states.shown_orbs.clone(),
    }
}
