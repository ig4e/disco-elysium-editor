use std::collections::HashMap;
use std::path::Path;
use crate::models::*;

/// Loads static game definition data from bundled JSON files.
pub struct GameDataService {
    pub skills: HashMap<String, GameSkill>,
    pub items: HashMap<String, GameItem>,
    pub thoughts: HashMap<String, GameThought>,
    pub task_variables: HashMap<String, GameVariable>,
    pub reputation_variables: HashMap<String, GameVariable>,
    pub character_variables: HashMap<String, GameVariable>,
    pub all_variables: HashMap<String, GameVariable>,
    pub xp_variables: HashMap<String, GameVariableXp>,
    pub major_npcs: HashMap<String, Actor>,
    pub skill_key_map: SkillKeyMap,
    pub is_loaded: bool,
}

impl Default for GameDataService {
    fn default() -> Self {
        Self {
            skills: HashMap::new(),
            items: HashMap::new(),
            thoughts: HashMap::new(),
            task_variables: HashMap::new(),
            reputation_variables: HashMap::new(),
            character_variables: HashMap::new(),
            all_variables: HashMap::new(),
            xp_variables: HashMap::new(),
            major_npcs: HashMap::new(),
            skill_key_map: SkillKeyMap {
                abilities: vec![],
                skills: vec![],
                equipment_slots: vec![],
                inventory_categories: vec![],
                modifier_cause_types: vec![],
                thought_states: vec![],
                healing_pool_types: vec![],
            },
            is_loaded: false,
        }
    }
}

impl GameDataService {
    pub fn load(&mut self, game_data_folder: &str) -> Result<(), String> {
        let folder = Path::new(game_data_folder);

        self.skills = load_json_array::<GameSkill>(&folder.join("actors_skills.json"))?
            .into_iter().map(|s| (s.name.clone(), s)).collect();

        self.items = load_json_array::<GameItem>(&folder.join("items_inventory.json"))?
            .into_iter().map(|i| (i.name.clone(), i)).collect();

        self.thoughts = load_json_array::<GameThought>(&folder.join("items_thoughts.json"))?
            .into_iter().map(|t| (t.name.clone(), t)).collect();

        self.task_variables = load_json_array::<GameVariable>(&folder.join("variables_tasks.json"))?
            .into_iter().map(|v| (v.name.clone(), v)).collect();

        self.reputation_variables = load_json_array::<GameVariable>(&folder.join("variables_reputation.json"))?
            .into_iter().map(|v| (v.name.clone(), v)).collect();

        self.character_variables = load_json_array::<GameVariable>(&folder.join("variables_character.json"))?
            .into_iter().map(|v| (v.name.clone(), v)).collect();

        self.all_variables = load_json_array::<GameVariable>(&folder.join("variables_all.json"))?
            .into_iter().map(|v| (v.name.clone(), v)).collect();

        self.xp_variables = load_json_array::<GameVariableXp>(&folder.join("variables_xp.json"))?
            .into_iter().map(|v| (v.name.clone(), v)).collect();

        self.major_npcs = load_json_array::<Actor>(&folder.join("actors_npcs_major.json"))?
            .into_iter().map(|a| (a.name.clone(), a)).collect();

        let key_map_path = folder.join("skill_key_map.json");
        let key_map_json = std::fs::read_to_string(&key_map_path)
            .map_err(|e| format!("Failed to read skill_key_map.json: {}", e))?;
        self.skill_key_map = serde_json::from_str(&key_map_json)
            .map_err(|e| format!("Failed to parse skill_key_map.json: {}", e))?;

        self.is_loaded = true;
        Ok(())
    }

    #[allow(dead_code)]
    pub fn get_skill_display_name(&self, save_key: &str) -> String {
        self.skill_key_map.find_by_save_key(save_key)
            .map(|m| m.display_name.clone())
            .unwrap_or_else(|| save_key.to_string())
    }

    pub fn get_skill_display_name_by_type(&self, skill_type: &str) -> String {
        self.skill_key_map.find_by_skill_type(skill_type)
            .map(|m| m.display_name.clone())
            .unwrap_or_else(|| skill_type.to_string())
    }

    pub fn get_task_description(&self, task_name: &str) -> String {
        self.task_variables.get(task_name)
            .map(|v| v.description.clone())
            .unwrap_or_else(|| task_name.to_string())
    }

    pub fn get_all_catalog_items(&self) -> Vec<CatalogItem> {
        let mut items: Vec<CatalogItem> = self.items.values().map(|item| CatalogItem {
            name: item.name.clone(),
            display_name: item.display_name.clone(),
            description: item.description.clone(),
            bonus: item.medium_text_value.clone(),
            is_quest_item: item.is_quest_item,
            is_cursed: item.is_cursed(),
            is_substance: item.is_substance_item(),
        }).collect();
        items.sort_by(|a, b| a.display_name.cmp(&b.display_name));
        items
    }
}

fn load_json_array<T: serde::de::DeserializeOwned>(path: &Path) -> Result<Vec<T>, String> {
    if !path.exists() {
        return Ok(vec![]);
    }
    let json = std::fs::read_to_string(path)
        .map_err(|e| format!("Failed to read {}: {}", path.display(), e))?;
    serde_json::from_str(&json)
        .map_err(|e| format!("Failed to parse {}: {}", path.display(), e))
}
