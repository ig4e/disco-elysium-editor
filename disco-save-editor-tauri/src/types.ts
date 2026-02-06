// Mirrors the Rust FullSaveState and related types

export interface SaveSummary {
  name: string;
  path: string;
  last_modified: string;
}

export interface FullSaveState {
  folder_path: string;
  base_name: string;
  xp_amount: number;
  level: number;
  skill_points: number;
  money: number;
  health: number;
  morale: number;
  day: number;
  hours: number;
  minutes: number;
  abilities: AbilityDisplay[];
  skills: SkillDisplay[];
  owned_items: InventoryItemDisplay[];
  bullets: number;
  thoughts: ThoughtDisplay[];
  tasks: TaskDisplay[];
  area_id: string;
  party_state: PartyState;
  hud_state: HudStateDisplay;
  game_mode: string;
  location_flags: LocationFlags;
  weather_preset: number;
  reputation: ReputationDisplay;
  lua_variable_count: number;
  failed_checks: WhiteCheckDisplay[];
  seen_checks: WhiteCheckDisplay[];
  containers: ContainerDisplay[];
  door_states: Record<string, boolean>;
  area_states: Record<string, number>;
  shown_orbs: Record<string, number>;
}

export interface AbilityDisplay {
  save_key: string;
  display_name: string;
  type_code: string;
  value: number;
  maximum_value: number;
  is_signature: boolean;
}

export interface SkillDisplay {
  save_key: string;
  display_name: string;
  type_code: string;
  ability_type: string;
  description: string;
  value: number;
  maximum_value: number;
  calculated_ability: number;
  rank_value: number;
  has_advancement: boolean;
  is_signature: boolean;
  modifier_count: number;
}

export interface InventoryItemDisplay {
  name: string;
  display_name: string;
  description: string;
  bonus: string;
  is_owned: boolean;
  is_equipped: boolean;
  equip_slot: string;
  is_quest_item: boolean;
  is_cursed: boolean;
  is_substance: boolean;
  substance_uses: number;
}

export interface ThoughtDisplay {
  name: string;
  display_name: string;
  description: string;
  bonus_while_processing: string;
  bonus_when_completed: string;
  completion_description: string;
  thought_type: string;
  time_to_internalize: number;
  requirement: string;
  is_cursed: boolean;
  state: string;
  time_left: number;
}

export interface TaskDisplay {
  task_name: string;
  description: string;
  acquired_time: string;
  is_resolved: boolean;
  is_new: boolean;
  subtasks: string[];
}

export interface PartyState {
  is_kim_in_party: boolean;
  is_kim_left_outside: boolean;
  is_kim_abandoned: boolean;
  is_kim_away_up_to_morning: boolean;
  is_kim_sleeping_in_his_room: boolean;
  is_kim_saying_good_morning: boolean;
  is_cuno_in_party: boolean;
  is_cuno_left_outside: boolean;
  is_cuno_abandoned: boolean;
  has_hangover: boolean;
  sleep_location: number;
  wait_location: number;
  cuno_wait_location: number;
  time_since_kim_went_sleeping: number;
  kim_last_arrival_location: number;
  cuno_last_arrival_location: number;
}

export interface HudStateDisplay {
  portrait_obscured: boolean;
  portrait_shaved: boolean;
  portrait_expression_stopped: boolean;
  portrait_fascist: boolean;
  charsheet_notification: boolean;
  inventory_notification: boolean;
  journal_notification: boolean;
  thc_notification: boolean;
  inv_clothes_notification: boolean;
  inv_pawnables_notification: boolean;
  inv_reading_notification: boolean;
  inv_tools_notification: boolean;
}

export interface LocationFlags {
  was_church_visited: boolean;
  was_fishing_village_visited: boolean;
  was_quicktravel_church_discovered: boolean;
  was_quicktravel_fishing_village_discovered: boolean;
}

export interface ReputationDisplay {
  communist: number;
  ultraliberal: number;
  moralist: number;
  nationalist: number;
  kim: number;
}

export interface WhiteCheckDisplay {
  key: string;
  flag_name: string;
  skill_type: string;
  skill_display_name: string;
  difficulty: number;
  last_skill_value: number;
  last_target_value: number;
  check_precondition: string;
  is_seen_only: boolean;
}

export interface ContainerDisplay {
  container_id: string;
  item_count: number;
  total_value: number;
  items: ContainerItemDisplay[];
}

export interface ContainerItemDisplay {
  name: string;
  probability: number;
  value: number;
  deviation: number;
  calculated_value: number;
  bonus_loot: boolean;
}

export interface LuaVariableDisplay {
  key: string;
  value: string;
  var_type: string;
  description: string;
}

export interface CatalogItem {
  name: string;
  display_name: string;
  description: string;
  bonus: string;
  is_quest_item: boolean;
  is_cursed: boolean;
  is_substance: boolean;
}

export interface SaveUpdatePayload {
  folder_path: string;
  base_name: string;
  xp_amount: number;
  level: number;
  skill_points: number;
  money: number;
  health: number;
  morale: number;
  day: number;
  hours: number;
  minutes: number;
  abilities: AbilityDisplay[];
  skills: SkillDisplay[];
  owned_items: InventoryItemDisplay[];
  bullets: number;
  thoughts: ThoughtDisplay[];
  area_id: string;
  party_state: PartyState;
  hud_state: HudStateDisplay;
  game_mode: string;
  location_flags: LocationFlags;
  weather_preset: number;
  reputation: ReputationDisplay;
  lua_edits: Record<string, string>;
  reset_check_keys: string[];
  reset_seen_check_keys: string[];
  door_states: Record<string, boolean>;
  area_states: Record<string, number>;
  shown_orbs: Record<string, number>;
}
