use serde::{Deserialize, Serialize};
use std::collections::HashMap;

// ─── Game Data Models (loaded from bundled JSON assets) ───

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Actor {
    pub id: i64,
    pub name: String,
    #[serde(default)]
    pub display_name: String,
    #[serde(default)]
    pub description: String,
    #[serde(default)]
    pub long_description: String,
    #[serde(default)]
    pub short_name: String,
    #[serde(default)]
    pub portrait: String,
    #[serde(default)]
    pub category: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GameItem {
    pub id: i64,
    pub name: String,
    #[serde(default)]
    pub display_name: String,
    #[serde(default)]
    pub description: String,
    #[serde(default)]
    pub item_type: f64,
    #[serde(default)]
    pub item_group: f64,
    #[serde(default)]
    pub item_value: f64,
    #[serde(default)]
    pub bonus: String,
    #[serde(default, alias = "MediumTextValue")]
    pub medium_text_value: String,
    #[serde(default)]
    pub is_quest_item: bool,
    #[serde(default)]
    pub autoequip: String,
    #[serde(default)]
    pub cursed: String,
    #[serde(default, alias = "isSubstance")]
    pub is_substance: String,
    #[serde(default, alias = "isConsumable")]
    pub is_consumable: String,
    #[serde(default, alias = "multipleAllowed")]
    pub multiple_allowed: String,
}

impl GameItem {
    pub fn is_cursed(&self) -> bool {
        self.cursed.eq_ignore_ascii_case("true")
    }
    pub fn is_substance_item(&self) -> bool {
        self.is_substance.eq_ignore_ascii_case("true")
    }
    #[allow(dead_code)]
    pub fn is_consumable_item(&self) -> bool {
        self.is_consumable.eq_ignore_ascii_case("true")
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GameSkill {
    pub id: i64,
    pub name: String,
    #[serde(default)]
    pub display_name: String,
    #[serde(default)]
    pub description: String,
    #[serde(default)]
    pub long_description: String,
    #[serde(default)]
    pub attribute_group: String,
    #[serde(default)]
    pub is_attribute: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GameThought {
    pub id: i64,
    pub name: String,
    #[serde(default)]
    pub display_name: String,
    #[serde(default)]
    pub description: String,
    #[serde(default)]
    pub thought_type: String,
    #[serde(default)]
    pub bonus_while_processing: String,
    #[serde(default)]
    pub bonus_when_completed: String,
    #[serde(default)]
    pub completion_description: String,
    #[serde(default)]
    pub time_to_internalize: f64,
    #[serde(default)]
    pub requirement: String,
    #[serde(default)]
    pub is_cursed: String,
}

impl GameThought {
    pub fn is_cursed_thought(&self) -> bool {
        self.is_cursed.eq_ignore_ascii_case("true")
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GameVariable {
    pub id: i64,
    pub name: String,
    #[serde(default)]
    pub initial_value: serde_json::Value,
    #[serde(default)]
    pub description: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GameVariableXp {
    pub id: i64,
    pub name: String,
    #[serde(default)]
    pub initial_value: serde_json::Value,
    #[serde(default)]
    pub description: String,
    #[serde(default)]
    pub xp_points: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SkillKeyMap {
    #[serde(default)]
    pub abilities: Vec<AbilityMapping>,
    #[serde(default)]
    pub skills: Vec<SkillMapping>,
    #[serde(default)]
    pub equipment_slots: Vec<String>,
    #[serde(default)]
    pub inventory_categories: Vec<String>,
    #[serde(default)]
    pub modifier_cause_types: Vec<String>,
    #[serde(default)]
    pub thought_states: Vec<String>,
    #[serde(default)]
    pub healing_pool_types: Vec<String>,
}

impl SkillKeyMap {
    pub fn ability_keys(&self) -> std::collections::HashSet<String> {
        self.abilities.iter().map(|a| a.save_key.clone()).collect()
    }
    pub fn skill_keys(&self) -> std::collections::HashSet<String> {
        self.skills.iter().map(|s| s.save_key.clone()).collect()
    }
    pub fn find_by_save_key(&self, key: &str) -> Option<&SkillMapping> {
        self.skills.iter().find(|s| s.save_key == key)
    }
    pub fn find_ability_by_save_key(&self, key: &str) -> Option<&AbilityMapping> {
        self.abilities.iter().find(|a| a.save_key == key)
    }
    pub fn find_by_skill_type(&self, skill_type: &str) -> Option<&SkillMapping> {
        self.skills.iter().find(|s| s.skill_type == skill_type)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AbilityMapping {
    pub save_key: String,
    pub skill_type: String,
    pub display_name: String,
    #[serde(default)]
    pub skills: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SkillMapping {
    pub save_key: String,
    pub skill_type: String,
    pub display_name: String,
    #[serde(default)]
    pub ability: String,
}

// ─── Save File Models ───

#[allow(dead_code)]
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct SaveData {
    pub folder_path: String,
    pub base_name: String,
    pub first: FirstFile,
    pub second: SecondFile,
    pub lua_database: HashMap<String, LuaValue>,
    pub states: StatesData,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(untagged)]
pub enum LuaValue {
    String(String),
    Number(f64),
    Boolean(bool),
    Table(HashMap<String, LuaValue>),
}

impl Default for LuaValue {
    fn default() -> Self {
        LuaValue::String(String::new())
    }
}

impl LuaValue {
    pub fn as_f64(&self) -> Option<f64> {
        match self {
            LuaValue::Number(n) => Some(*n),
            _ => None,
        }
    }
    #[allow(dead_code)]
    pub fn as_bool(&self) -> Option<bool> {
        match self {
            LuaValue::Boolean(b) => Some(*b),
            _ => None,
        }
    }
    #[allow(dead_code)]
    pub fn as_str(&self) -> Option<&str> {
        match self {
            LuaValue::String(s) => Some(s),
            _ => None,
        }
    }
    #[allow(dead_code)]
    pub fn as_table(&self) -> Option<&HashMap<String, LuaValue>> {
        match self {
            LuaValue::Table(t) => Some(t),
            _ => None,
        }
    }
    pub fn type_name(&self) -> &'static str {
        match self {
            LuaValue::String(_) => "String",
            LuaValue::Number(_) => "Number",
            LuaValue::Boolean(_) => "Boolean",
            LuaValue::Table(_) => "Table",
        }
    }
    pub fn to_display_string(&self) -> String {
        match self {
            LuaValue::String(s) => s.clone(),
            LuaValue::Number(n) => n.to_string(),
            LuaValue::Boolean(b) => b.to_string(),
            LuaValue::Table(_) => "[Table]".to_string(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct FirstFile {
    #[serde(default, alias = "areaId")]
    pub area_id: String,
    #[serde(default, alias = "partyState")]
    pub party_state: PartyState,
    #[serde(default, alias = "fowUnrevealersStatusCache")]
    pub fow_unrevealers_status_cache: HashMap<String, String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct PartyState {
    #[serde(default, alias = "isKimInParty")]
    pub is_kim_in_party: bool,
    #[serde(default, alias = "isKimLeftOutside")]
    pub is_kim_left_outside: bool,
    #[serde(default, alias = "isKimAbandoned")]
    pub is_kim_abandoned: bool,
    #[serde(default, alias = "isKimAwayUpToMorning")]
    pub is_kim_away_up_to_morning: bool,
    #[serde(default, alias = "isKimSleepingInHisRoom")]
    pub is_kim_sleeping_in_his_room: bool,
    #[serde(default, alias = "isKimSayingGoodMorning")]
    pub is_kim_saying_good_morning: bool,
    #[serde(default, alias = "isCunoInParty")]
    pub is_cuno_in_party: bool,
    #[serde(default, alias = "isCunoLeftOutside")]
    pub is_cuno_left_outside: bool,
    #[serde(default, alias = "isCunoAbandoned")]
    pub is_cuno_abandoned: bool,
    #[serde(default, alias = "hasHangover")]
    pub has_hangover: bool,
    #[serde(default, alias = "sleepLocation")]
    pub sleep_location: i64,
    #[serde(default, alias = "waitLocation")]
    pub wait_location: i64,
    #[serde(default, alias = "cunoWaitLocation")]
    pub cuno_wait_location: i64,
    #[serde(default, alias = "timeSinceKimWentSleepingInHisRoom")]
    pub time_since_kim_went_sleeping: i64,
    #[serde(default, alias = "kimLastArrivalLocation")]
    pub kim_last_arrival_location: i64,
    #[serde(default, alias = "cunoLastArrivalLocation")]
    pub cuno_last_arrival_location: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct SecondFile {
    #[serde(default, alias = "variousItemsHolder")]
    pub various_items_holder: VariousItemsHolder,
    #[serde(default, alias = "sunshineClockTimeHolder")]
    pub sunshine_clock_time_holder: SunshineClockTimeHolder,
    #[serde(default, alias = "playerCharacter")]
    pub player_character: PlayerCharacter,
    #[serde(default, alias = "hudState")]
    pub hud_state: HudState,
    #[serde(default, alias = "aquiredJournalTasks")]
    pub acquired_journal_tasks: AcquiredJournalTasks,
    #[serde(default, alias = "failedWhiteChecksHolder")]
    pub failed_white_checks_holder: FailedWhiteChecksHolder,
    #[serde(default, alias = "weatherState")]
    pub weather_state: WeatherState,
    #[serde(default, alias = "inventoryState")]
    pub inventory_state: InventoryState,
    #[serde(default, alias = "thoughtCabinetState")]
    pub thought_cabinet_state: ThoughtCabinetState,
    #[serde(default, alias = "containerSourceState")]
    pub container_source_state: ContainerSourceState,
    #[serde(default, alias = "gameModeState")]
    pub game_mode_state: GameModeState,
    // Character sheet is parsed separately
    #[serde(default, alias = "characterSheet")]
    pub character_sheet_raw: serde_json::Value,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct CharacterSheet {
    pub abilities: HashMap<String, SkillEntry>,
    pub skills: HashMap<String, SkillEntry>,
    #[serde(default)]
    pub gained_items: Vec<String>,
    #[serde(default)]
    pub equipped_items: Vec<String>,
    #[serde(default)]
    pub gained_thoughts: Vec<String>,
    #[serde(default)]
    pub cooking_thoughts: Vec<String>,
    #[serde(default)]
    pub fixed_thoughts: Vec<String>,
    #[serde(default)]
    pub forgotten_thoughts: Vec<String>,
    #[serde(default)]
    pub selected_panel_name: String,
    #[serde(default)]
    pub skill_modifier_cause_map: HashMap<String, Vec<ModifierEntry>>,
    #[serde(default)]
    pub ability_modifier_cause_map: HashMap<String, Vec<ModifierEntry>>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct SkillEntry {
    #[serde(default, alias = "skillType")]
    pub skill_type: String,
    #[serde(default, alias = "abilityType")]
    pub ability_type: String,
    #[serde(default)]
    pub dirty: bool,
    #[serde(default)]
    pub value: i64,
    #[serde(default, alias = "valueWithoutPerceptionsSubSkills")]
    pub value_without_perceptions_sub_skills: i64,
    #[serde(default, alias = "damageValue")]
    pub damage_value: i64,
    #[serde(default, alias = "maximumValue")]
    pub maximum_value: i64,
    #[serde(default, alias = "calculatedAbility")]
    pub calculated_ability: i64,
    #[serde(default, alias = "rankValue")]
    pub rank_value: i64,
    #[serde(default, alias = "hasAdvancement")]
    pub has_advancement: bool,
    #[serde(default, alias = "isSignature")]
    pub is_signature: bool,
    #[serde(default)]
    pub modifiers: serde_json::Value,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ModifierEntry {
    #[serde(default, alias = "type")]
    pub modifier_type: String,
    #[serde(default)]
    pub amount: i64,
    #[serde(default)]
    pub explanation: String,
    #[serde(default, alias = "skillType")]
    pub skill_type: String,
    #[serde(default, alias = "modifierCause")]
    pub modifier_cause: ModifierCause,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ModifierCause {
    #[serde(default, alias = "ModifierKey")]
    pub modifier_key: String,
    #[serde(default, alias = "ModifierCauseType")]
    pub modifier_cause_type: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct VariousItemsHolder {
    #[serde(default, alias = "Obsessions")]
    pub obsessions: Vec<String>,
    #[serde(default, alias = "DoorStates")]
    pub door_states: HashMap<String, bool>,
    #[serde(default, alias = "BuildNumber")]
    pub build_number: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct SunshineClockTimeHolder {
    #[serde(default)]
    pub time: GameTimestamp,
    #[serde(default, alias = "timeOverride")]
    pub time_override: serde_json::Value,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct GameTimestamp {
    #[serde(default, alias = "dayCounter")]
    pub day_counter: i64,
    #[serde(default, alias = "realDayCounter")]
    pub real_day_counter: i64,
    #[serde(default, alias = "dayMinutes")]
    pub day_minutes: i64,
    #[serde(default)]
    pub seconds: i64,
}

impl GameTimestamp {
    pub fn hours(&self) -> i64 { self.day_minutes / 60 }
    pub fn minutes(&self) -> i64 { self.day_minutes % 60 }
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct PlayerCharacter {
    #[serde(default, alias = "XpAmount")]
    pub xp_amount: i64,
    #[serde(default, alias = "Level")]
    pub level: i64,
    #[serde(default, alias = "SkillPoints")]
    pub skill_points: i64,
    #[serde(default, alias = "Money")]
    pub money: i64,
    #[serde(default, alias = "StockValue")]
    pub stock_value: i64,
    #[serde(default, alias = "NewPointsToSpend")]
    pub new_points_to_spend: bool,
    #[serde(default, alias = "healingPools")]
    pub healing_pools: HealingPools,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HealingPools {
    #[serde(default, alias = "ENDURANCE")]
    pub endurance: i64,
    #[serde(default, alias = "VOLITION")]
    pub volition: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HudState {
    #[serde(default, alias = "tequilaPortraitObscured")]
    pub tequila_portrait_obscured: bool,
    #[serde(default, alias = "tequilaPortraitShaved")]
    pub tequila_portrait_shaved: bool,
    #[serde(default, alias = "tequilaPortraitExpressionStopped")]
    pub tequila_portrait_expression_stopped: bool,
    #[serde(default, alias = "tequilaPortraitFascist")]
    pub tequila_portrait_fascist: bool,
    #[serde(default, alias = "charsheetNotification")]
    pub charsheet_notification: bool,
    #[serde(default, alias = "inventoryNotification")]
    pub inventory_notification: bool,
    #[serde(default, alias = "journalNotification")]
    pub journal_notification: bool,
    #[serde(default, alias = "thcNotification")]
    pub thc_notification: bool,
    #[serde(default, alias = "invClothesNotification")]
    pub inv_clothes_notification: bool,
    #[serde(default, alias = "invPawnablesNotification")]
    pub inv_pawnables_notification: bool,
    #[serde(default, alias = "invReadingNotification")]
    pub inv_reading_notification: bool,
    #[serde(default, alias = "invToolsNotification")]
    pub inv_tools_notification: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct AcquiredJournalTasks {
    #[serde(default, alias = "TaskAquisitions")]
    pub task_acquisitions: HashMap<String, GameTimestamp>,
    #[serde(default, alias = "TaskResolutions")]
    pub task_resolutions: HashMap<String, serde_json::Value>,
    #[serde(default, alias = "SubtaskAquisitions")]
    pub subtask_acquisitions: HashMap<String, HashMap<String, GameTimestamp>>,
    #[serde(default, alias = "TaskNewStates")]
    pub task_new_states: HashMap<String, bool>,
    #[serde(default, alias = "ChecksWithNotifications")]
    pub checks_with_notifications: serde_json::Value,
    #[serde(default, alias = "LastActiveTask")]
    pub last_active_task: String,
    #[serde(default, alias = "LastDoneTask")]
    pub last_done_task: String,
    #[serde(default, alias = "TasksTabNotifyIcon")]
    pub tasks_tab_notify_icon: bool,
    #[serde(default, alias = "wasChurchVisited")]
    pub was_church_visited: bool,
    #[serde(default, alias = "wasFishingVillageVisited")]
    pub was_fishing_village_visited: bool,
    #[serde(default, alias = "wasQuicktravelChurchDiscovered")]
    pub was_quicktravel_church_discovered: bool,
    #[serde(default, alias = "wasQuicktravelFishingVillageDiscovered")]
    pub was_quicktravel_fishing_village_discovered: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct FailedWhiteChecksHolder {
    #[serde(default, alias = "ReopenedWhiteChecksByActorName")]
    pub reopened_white_checks: HashMap<String, serde_json::Value>,
    #[serde(default, alias = "WhiteCheckCache")]
    pub white_check_cache: HashMap<String, serde_json::Value>,
    #[serde(default, alias = "SeenWhiteCheckCache")]
    pub seen_white_check_cache: HashMap<String, WhiteCheck>,
    #[serde(default, alias = "ChecksBySkill")]
    pub checks_by_skill: HashMap<String, serde_json::Value>,
    #[serde(default, alias = "ChecksByVariable")]
    pub checks_by_variable: HashMap<String, serde_json::Value>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct WhiteCheck {
    #[serde(default, alias = "FlagName")]
    pub flag_name: String,
    #[serde(default, alias = "SkillType")]
    pub skill_type: String,
    #[serde(default, alias = "LastSkillValue")]
    pub last_skill_value: i64,
    #[serde(default, alias = "LastTargetValue")]
    pub last_target_value: i64,
    #[serde(default)]
    pub difficulty: i64,
    #[serde(default, alias = "checkPrecondition")]
    pub check_precondition: String,
    #[serde(default, alias = "isOnlySeen")]
    pub is_only_seen: bool,
    #[serde(default, alias = "checkTargetArticyId")]
    pub check_target_articy_id: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct WeatherState {
    #[serde(default, alias = "weatherPreset")]
    pub weather_preset: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct InventoryState {
    #[serde(default, alias = "itemListState")]
    pub item_list_state: Vec<ItemState>,
    #[serde(default, alias = "inventoryViewState")]
    pub inventory_view_state: InventoryViewState,
    #[serde(default, alias = "wearingBodysuit")]
    pub wearing_bodysuit: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ItemState {
    #[serde(default, alias = "itemName")]
    pub item_name: String,
    #[serde(default, alias = "isFresh")]
    pub is_fresh: bool,
    #[serde(default, alias = "substanceUses")]
    pub substance_uses: i64,
    #[serde(default, alias = "substanceTimeLeft")]
    pub substance_time_left: i64,
    #[serde(default, alias = "StackItems")]
    pub stack_items: serde_json::Value,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct InventoryViewState {
    #[serde(default)]
    pub equipment: HashMap<String, String>,
    #[serde(default)]
    pub inventory: HashMap<String, serde_json::Value>,
    #[serde(default)]
    pub bullets: i64,
    #[serde(default)]
    pub keys: Vec<String>,
    #[serde(default, alias = "lastSelectedItem")]
    pub last_selected_item: String,
}

#[allow(dead_code)]
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct InventorySlot {
    #[serde(default, alias = "Key")]
    pub key: i64,
    #[serde(default, alias = "Value")]
    pub value: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ThoughtCabinetState {
    #[serde(default, alias = "thoughtListState")]
    pub thought_list_state: Vec<ThoughtState>,
    #[serde(default, alias = "thoughtCabinetViewState")]
    pub thought_cabinet_view_state: ThoughtCabinetViewState,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ThoughtState {
    #[serde(default)]
    pub name: String,
    #[serde(default, alias = "isFresh")]
    pub is_fresh: bool,
    #[serde(default)]
    pub state: String,
    #[serde(default, alias = "timeLeft")]
    pub time_left: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ThoughtCabinetViewState {
    #[serde(default, alias = "slotStates")]
    pub slot_states: Vec<SlotState>,
    #[serde(default, alias = "selectedProjectName")]
    pub selected_project_name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct SlotState {
    #[serde(default, alias = "Item1")]
    pub item1: String,
    #[serde(default, alias = "Item2")]
    pub item2: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ContainerSourceState {
    #[serde(default, alias = "itemRegistry")]
    pub item_registry: HashMap<String, Vec<ContainerItem>>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ContainerItem {
    #[serde(default)]
    pub name: String,
    #[serde(default)]
    pub probability: f64,
    #[serde(default)]
    pub value: i64,
    #[serde(default)]
    pub deviation: i64,
    #[serde(default, alias = "calculatedValue")]
    pub calculated_value: i64,
    #[serde(default, alias = "bonusLoot")]
    pub bonus_loot: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct GameModeState {
    #[serde(default, alias = "gameMode")]
    pub game_mode: String,
    #[serde(default, alias = "wasSwitched")]
    pub was_switched: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct StatesData {
    pub area_states: HashMap<String, i64>,
    pub shown_orbs: HashMap<String, i64>,
}

// ─── Frontend DTOs (sent to React) ───

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SaveSummary {
    pub name: String,
    pub path: String,
    pub last_modified: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FullSaveState {
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
    // Journal
    pub tasks: Vec<TaskDisplay>,
    // Party
    pub area_id: String,
    pub party_state: PartyState,
    pub hud_state: HudStateDisplay,
    pub game_mode: String,
    pub location_flags: LocationFlags,
    // World
    pub weather_preset: i64,
    pub reputation: ReputationDisplay,
    pub lua_variable_count: usize,
    // White Checks
    pub failed_checks: Vec<WhiteCheckDisplay>,
    pub seen_checks: Vec<WhiteCheckDisplay>,
    // Containers
    pub containers: Vec<ContainerDisplay>,
    // States
    pub door_states: HashMap<String, bool>,
    pub area_states: HashMap<String, i64>,
    pub shown_orbs: HashMap<String, i64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AbilityDisplay {
    pub save_key: String,
    pub display_name: String,
    pub type_code: String,
    pub value: i64,
    pub maximum_value: i64,
    pub is_signature: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SkillDisplay {
    pub save_key: String,
    pub display_name: String,
    pub type_code: String,
    pub ability_type: String,
    pub description: String,
    pub value: i64,
    pub maximum_value: i64,
    pub calculated_ability: i64,
    pub rank_value: i64,
    pub has_advancement: bool,
    pub is_signature: bool,
    pub modifier_count: usize,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct InventoryItemDisplay {
    pub name: String,
    pub display_name: String,
    pub description: String,
    pub bonus: String,
    pub is_owned: bool,
    pub is_equipped: bool,
    pub equip_slot: String,
    pub is_quest_item: bool,
    pub is_cursed: bool,
    pub is_substance: bool,
    pub substance_uses: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ThoughtDisplay {
    pub name: String,
    pub display_name: String,
    pub description: String,
    pub bonus_while_processing: String,
    pub bonus_when_completed: String,
    pub completion_description: String,
    pub thought_type: String,
    pub time_to_internalize: f64,
    pub requirement: String,
    pub is_cursed: bool,
    pub state: String,
    pub time_left: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TaskDisplay {
    pub task_name: String,
    pub description: String,
    pub acquired_time: String,
    pub is_resolved: bool,
    pub is_new: bool,
    pub subtasks: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HudStateDisplay {
    pub portrait_obscured: bool,
    pub portrait_shaved: bool,
    pub portrait_expression_stopped: bool,
    pub portrait_fascist: bool,
    pub charsheet_notification: bool,
    pub inventory_notification: bool,
    pub journal_notification: bool,
    pub thc_notification: bool,
    pub inv_clothes_notification: bool,
    pub inv_pawnables_notification: bool,
    pub inv_reading_notification: bool,
    pub inv_tools_notification: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LocationFlags {
    pub was_church_visited: bool,
    pub was_fishing_village_visited: bool,
    pub was_quicktravel_church_discovered: bool,
    pub was_quicktravel_fishing_village_discovered: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ReputationDisplay {
    pub communist: f64,
    pub ultraliberal: f64,
    pub moralist: f64,
    pub nationalist: f64,
    pub kim: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WhiteCheckDisplay {
    pub key: String,
    pub flag_name: String,
    pub skill_type: String,
    pub skill_display_name: String,
    pub difficulty: i64,
    pub last_skill_value: i64,
    pub last_target_value: i64,
    pub check_precondition: String,
    pub is_seen_only: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ContainerDisplay {
    pub container_id: String,
    pub item_count: usize,
    pub total_value: i64,
    pub items: Vec<ContainerItemDisplay>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ContainerItemDisplay {
    pub name: String,
    pub probability: f64,
    pub value: i64,
    pub deviation: i64,
    pub calculated_value: i64,
    pub bonus_loot: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LuaVariableDisplay {
    pub key: String,
    pub value: String,
    pub var_type: String,
    pub description: String,
}

// ─── Catalog item for adding items ───

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CatalogItem {
    pub name: String,
    pub display_name: String,
    pub description: String,
    pub bonus: String,
    pub is_quest_item: bool,
    pub is_cursed: bool,
    pub is_substance: bool,
}
