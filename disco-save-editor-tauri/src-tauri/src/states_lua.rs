use regex::Regex;
use crate::models::StatesData;

/// Parse a .states.lua text file
pub fn parse_states(content: &str) -> StatesData {
    let mut data = StatesData::default();

    let area_re = Regex::new(r#"AreaState\["(.+?)"\]\s*=\s*\{LocationState=(\d+)\}"#).unwrap();
    let orb_re = Regex::new(r#"ShownOrbs\["(.+?)"\]\s*=\s*\{OrbSeen=(\d+)\}"#).unwrap();

    for cap in area_re.captures_iter(content) {
        let key = cap[1].to_string();
        let val: i64 = cap[2].parse().unwrap_or(0);
        data.area_states.insert(key, val);
    }

    for cap in orb_re.captures_iter(content) {
        let key = cap[1].to_string();
        let val: i64 = cap[2].parse().unwrap_or(0);
        data.shown_orbs.insert(key, val);
    }

    data
}

/// Serialize StatesData back to .states.lua format
pub fn serialize_states(data: &StatesData) -> String {
    let mut sb = String::new();

    let mut area_keys: Vec<&String> = data.area_states.keys().collect();
    area_keys.sort();
    for key in area_keys {
        let val = data.area_states[key];
        sb.push_str(&format!("AreaState[\"{}\"]={{LocationState={}}};\n", key, val));
    }

    let mut orb_keys: Vec<&String> = data.shown_orbs.keys().collect();
    orb_keys.sort();
    for key in orb_keys {
        let val = data.shown_orbs[key];
        sb.push_str(&format!("ShownOrbs[\"{}\"]={{OrbSeen={}}};\n", key, val));
    }

    sb
}
