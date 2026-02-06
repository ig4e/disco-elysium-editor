"""
Disco Elysium Game Asset Extractor
===================================
Extracts the complete game database from Unity asset bundles using UnityPy.
Produces clean, categorized JSON files for use in a save file editor.

Data sources:
  - dialoguebundle: PixelCrushers DialogueDatabase with ALL game definitions
  - substance bundle: drug/substance data  
  - localization bundles: translated text

Output structure:
  game_assets/
    actors_skills.json       - 24 skills + 4 attributes with descriptions/bonuses
    actors_npcs_major.json   - 125 major named NPCs
    actors_npcs_minor.json   - 266 minor/unnamed NPCs
    actors_player.json       - Player character ("You")
    actors_voices.json       - Non-skill brain voices (Ancient Reptilian Brain, etc.)
    items_inventory.json     - ~206 inventory items with descriptions
    items_thoughts.json      - 53 Thought Cabinet thoughts with full bonus data
    variables_tasks.json     - 1131 task/quest variables with descriptions
    variables_xp.json        - 689 XP reward variables with point values
    variables_reputation.json- 21 political alignment counters
    variables_character.json - 111 character state variables
    variables_stats.json     - 41 stats variables
    variables_inventory.json - 266 inventory state variables
    variables_locations.json - Location-specific state variables
    variables_all.json       - Complete variable reference (10,645 vars)
    conversations_index.json - 1,501 conversation metadata
    _manifest.json           - Summary of all extracted data
"""
import UnityPy
import json
import os
import sys
from collections import defaultdict

GAME_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AA_DIR = os.path.join(GAME_DIR, "disco_Data", "StreamingAssets", "aa", "StandaloneWindows64")
OUTPUT_DIR = os.path.join(GAME_DIR, "output", "game_assets")

# Known skill names (24 skills + 4 attributes)
SKILL_NAMES = {
    # Attributes
    "Fysique", "Intellect", "Psyche", "Motorics",
    # INT skills
    "Logic", "Encyclopedia", "Rhetoric", "Drama",
    "Conceptualization", "Visual Calculus",
    # PSY skills
    "Volition", "Inland Empire", "Empathy", "Authority",
    "Esprit de Corps", "Suggestion",
    # FYS skills
    "Endurance", "Pain Threshold", "Physical Instrument",
    "Shivers", "Electrochemistry", "Half Light",
    # MOT skills
    "Hand/Eye Coordination", "Perception",
    "Reaction Speed", "Savoir Faire",
    "Interfacing", "Composure",
}

# Color mapping for actors
ATTR_COLORS = {
    2.0: "INT",  # Intellect group
    3.0: "PSY",  # Psyche group
    4.0: "FYS",  # Fysique group
    5.0: "MOT",  # Motorics group
}

# Non-skill brain voices that appear in color group 5
BRAIN_VOICES = {
    "Ancient Reptilian Brain", "Limbic System", "Spinal Cord",
    "Tutorial Agent", "Horrific Necktie", "Beautiful Necktie",
}

def get_field(fields, title, default=None):
    """Extract a field value from a PixelCrushers field list."""
    for f in fields:
        if f.get("title") == title:
            return f.get("value", default)
    return default

def fields_to_dict(fields):
    """Convert a PixelCrushers field list to a clean dict."""
    result = {}
    for f in fields:
        title = f.get("title", "")
        value = f.get("value", "")
        ftype = f.get("type", 0)
        # Parse typed values
        if ftype == 1:  # Float
            try: value = float(value)
            except: pass
        elif ftype == 3:  # Boolean
            value = str(value).lower() in ("true", "1")
        if value != "" and value is not None:
            result[title] = value
    return result

def extract_raw_db():
    """Load or extract the raw dialogue database."""
    cache_path = os.path.join(OUTPUT_DIR, "dialogue_db_typetree.json")
    
    if os.path.exists(cache_path):
        print(f"Loading cached database ({os.path.getsize(cache_path)/1024/1024:.1f} MB)...")
        with open(cache_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    # Extract from bundle
    bundle_path = None
    for f in os.listdir(AA_DIR):
        if f.startswith("dialoguebundle") and f.endswith(".bundle"):
            bundle_path = os.path.join(AA_DIR, f)
            break
    
    if not bundle_path:
        print("ERROR: dialoguebundle not found!")
        sys.exit(1)
    
    print(f"Extracting from {os.path.basename(bundle_path)}...")
    env = UnityPy.load(bundle_path)
    
    for obj in env.objects:
        if obj.type.name == "MonoBehaviour":
            tree = obj.read_typetree()
            os.makedirs(OUTPUT_DIR, exist_ok=True)
            with open(cache_path, 'w', encoding='utf-8') as f:
                json.dump(tree, f, indent=2, ensure_ascii=False, 
                         default=lambda o: f"<{len(o)} bytes>" if isinstance(o, bytes) else str(o))
            print(f"Cached to {cache_path}")
            return tree
    
    print("ERROR: No MonoBehaviour found in dialogue bundle!")
    sys.exit(1)

def process_actors(raw_actors):
    """Classify actors into skills, NPCs, player, and voices."""
    skills = []
    npcs_major = []
    npcs_minor = []
    player = []
    voices = []
    
    for actor in raw_actors:
        fields = fields_to_dict(actor.get("fields", []))
        name = fields.get("Name", "")
        color = fields.get("color", None)
        
        entry = {
            "id": actor["id"],
            "name": name,
            "display_name": fields.get("Display Name", name),
            "articy_id": fields.get("Articy Id", ""),
            "short_name": fields.get("character_short_name", ""),
            "description": fields.get("short_description", ""),
            "long_description": fields.get("LongDescription", ""),
            "portrait": fields.get("Pictures", ""),
            "is_female": fields.get("IsFemale", False),
        }
        
        # Player character
        if color == 7.0 or name == "You":
            entry["category"] = "player"
            player.append(entry)
        # Skills and attributes (color 2-4, or color 5 if a known skill)
        elif name in SKILL_NAMES:
            attr_group = ATTR_COLORS.get(color, "MOT")
            entry["category"] = "skill"
            entry["attribute_group"] = attr_group
            entry["is_attribute"] = name in ("Fysique", "Intellect", "Psyche", "Motorics")
            skills.append(entry)
        # Brain voices (color 5, not a skill)
        elif name in BRAIN_VOICES or (color == 5.0 and name not in SKILL_NAMES):
            entry["category"] = "voice"
            voices.append(entry)
        # Major NPCs (have a color=1.0 or have portraits)
        elif color == 1.0:
            entry["category"] = "npc_major"
            npcs_major.append(entry)
        # Minor NPCs (no color)
        else:
            entry["category"] = "npc_minor"
            npcs_minor.append(entry)
    
    return skills, npcs_major, npcs_minor, player, voices

def process_items(raw_items):
    """Classify items into inventory items and thoughts."""
    inventory = []
    thoughts = []
    
    for item in raw_items:
        fields = fields_to_dict(item.get("fields", []))
        name = fields.get("Name", "")
        
        base = {
            "id": item["id"],
            "name": name,
            "display_name": fields.get("Display Name", name),
            "articy_id": fields.get("Articy Id", ""),
            "short_name": fields.get("character_short_name", ""),
            "description": fields.get("description", ""),
        }
        
        if fields.get("isThought") or fields.get("thoughtType"):
            thought_type_map = {
                1.0: "INT", 2.0: "PSY", 3.0: "MOT", 
                4.0: "FYS", 5.0: "other"
            }
            tt = fields.get("thoughtType", 0)
            base.update({
                "category": "thought",
                "thought_type": thought_type_map.get(tt, str(tt)),
                "thought_type_raw": tt,
                "bonus_while_processing": fields.get("bonus", ""),
                "bonus_when_completed": fields.get("fixtureBonus", ""),
                "completion_description": fields.get("fixtureDescription", ""),
                "time_to_internalize": fields.get("timeLeft", 0),
                "requirement": fields.get("requirement", ""),
                "is_cursed": fields.get("Cursed", False),
            })
            thoughts.append(base)
        else:
            base.update({
                "category": "item",
                "item_type": fields.get("itemType", ""),
                "item_group": fields.get("itemGroup", ""),
                "item_value": fields.get("itemValue", ""),
                "equip_slot": fields.get("equipOrb", ""),
                "bonus": fields.get("bonus", ""),
                "skill_modifier": fields.get("skillModifier", ""),
                "is_quest_item": fields.get("isQuestItem", False),
            })
            # Include all remaining fields for completeness
            for k, v in fields.items():
                if k not in base and k not in ("Name", "Display Name", "IsItem", "Is Item"):
                    base[k] = v
            inventory.append(base)
    
    return inventory, thoughts

def process_variables(raw_variables):
    """Categorize variables by prefix and function."""
    all_vars = []
    categorized = defaultdict(list)
    
    # Location prefixes
    LOCATION_PREFIXES = {
        "whirling", "plaza", "yard", "seafort", "village", "jam", "doomed",
        "coast", "pier", "cargo", "gates", "ice", "boardwalk", "church",
        "apt", "canal", "backyard", "tc", "kimswitch", "southcoast",
        "containeryard", "lair", "office", "hq", "kineema", "lands-end",
        "nethouse", "pawnshop", "shack", "walkway", "westcoast",
        "dream", "daychange", "lifeline", "initiation", "evrart",
        "joyce", "measurehead", "racist", "fritte", "garys-apartment",
    }
    
    for var in raw_variables:
        fields = fields_to_dict(var.get("fields", []))
        name = fields.get("Name", "")
        initial = fields.get("Initial Value", "")
        desc = fields.get("Description", "")
        
        entry = {
            "id": var["id"],
            "name": name,
            "initial_value": initial,
            "description": desc,
        }
        all_vars.append(entry)
        
        # Categorize
        prefix = name.split(".")[0] if "." in name else "_other"
        
        if prefix == "TASK":
            categorized["tasks"].append(entry)
        elif prefix == "XP":
            # XP vars often have point value in description
            entry["xp_points"] = desc if desc else ""
            categorized["xp"].append(entry)
        elif prefix == "reputation":
            categorized["reputation"].append(entry)
        elif prefix == "character":
            categorized["character"].append(entry)
        elif prefix == "stats":
            categorized["stats"].append(entry)
        elif prefix == "inventory":
            categorized["inventory"].append(entry)
        elif prefix == "auto":
            categorized["auto"].append(entry)
        elif prefix == "globals":
            categorized["globals"].append(entry)
        elif prefix == "conventions":
            categorized["conventions"].append(entry)
        elif prefix == "tutorials":
            categorized["tutorials"].append(entry)
        elif prefix.lower() in LOCATION_PREFIXES or prefix == "Plaza":
            entry["location"] = prefix
            categorized["locations"].append(entry)
        else:
            # Remaining location vars we missed
            categorized["locations"].append(entry)
            entry["location"] = prefix
    
    return all_vars, dict(categorized)

def process_conversations(raw_conversations):
    """Build conversation index with metadata."""
    index = []
    
    for conv in raw_conversations:
        fields = fields_to_dict(conv.get("fields", []))
        entries = conv.get("dialogueEntries", [])
        
        # Count skill checks in this conversation
        skill_checks = 0
        actors_involved = set()
        for de in entries:
            cond = de.get("conditionsString", "")
            if cond and ("Variable" in cond or "skill" in cond.lower()):
                skill_checks += 1
            aid = de.get("ActorID")
            if aid:
                actors_involved.add(aid)
        
        entry = {
            "id": conv["id"],
            "title": fields.get("Title", ""),
            "description": fields.get("Description", ""),
            "articy_id": fields.get("Articy Id", ""),
            "num_entries": len(entries),
            "num_skill_checks": skill_checks,
            "actor_ids": sorted(actors_involved),
        }
        index.append(entry)
    
    return index

def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    # Step 1: Load raw database
    db = extract_raw_db()
    print(f"\nDatabase: {db.get('version', '?')} by {db.get('author', '?')}")
    
    # Step 2: Process actors
    print(f"\nProcessing {len(db['actors'])} actors...")
    skills, npcs_major, npcs_minor, player, voices = process_actors(db["actors"])
    
    # Step 3: Process items
    print(f"Processing {len(db['items'])} items...")
    inventory, thoughts = process_items(db["items"])
    
    # Step 4: Process variables
    print(f"Processing {len(db['variables'])} variables...")
    all_vars, var_categories = process_variables(db["variables"])
    
    # Step 5: Process conversations
    print(f"Processing {len(db['conversations'])} conversations...")
    conversations = process_conversations(db["conversations"])
    
    # Summary
    print(f"\n{'='*60}")
    print(f"EXTRACTION COMPLETE")
    print(f"{'='*60}")
    print(f"  Skills & Attributes: {len(skills)}")
    print(f"  Major NPCs: {len(npcs_major)}")
    print(f"  Minor NPCs: {len(npcs_minor)}")
    print(f"  Brain Voices: {len(voices)}")
    print(f"  Player: {len(player)}")
    print(f"  Inventory Items: {len(inventory)}")
    print(f"  Thoughts: {len(thoughts)}")
    print(f"  Conversations: {len(conversations)}")
    total_entries = sum(c["num_entries"] for c in conversations)
    print(f"  Total Dialogue Entries: {total_entries}")
    print(f"  Variables (total): {len(all_vars)}")
    for cat, vars in sorted(var_categories.items()):
        print(f"    {cat}: {len(vars)}")
    
    # Save files
    files = {
        "actors_skills.json": skills,
        "actors_npcs_major.json": npcs_major,
        "actors_npcs_minor.json": npcs_minor,
        "actors_player.json": player,
        "actors_voices.json": voices,
        "items_inventory.json": inventory,
        "items_thoughts.json": thoughts,
        "conversations_index.json": conversations,
        "variables_tasks.json": var_categories.get("tasks", []),
        "variables_xp.json": var_categories.get("xp", []),
        "variables_reputation.json": var_categories.get("reputation", []),
        "variables_character.json": var_categories.get("character", []),
        "variables_stats.json": var_categories.get("stats", []),
        "variables_inventory.json": var_categories.get("inventory", []),
        "variables_locations.json": var_categories.get("locations", []),
        "variables_auto.json": var_categories.get("auto", []),
        "variables_globals.json": var_categories.get("globals", []),
        "variables_all.json": all_vars,
    }
    
    print(f"\nSaving to {OUTPUT_DIR}...")
    manifest = {"version": db.get("version", ""), "author": db.get("author", ""), "files": {}}
    
    for filename, data in files.items():
        path = os.path.join(OUTPUT_DIR, filename)
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        size = os.path.getsize(path)
        manifest["files"][filename] = {
            "count": len(data) if isinstance(data, list) else "N/A",
            "size_kb": round(size / 1024, 1)
        }
        print(f"  {filename}: {len(data) if isinstance(data, list) else '?'} entries ({size/1024:.1f} KB)")
    
    # Save manifest
    manifest_path = os.path.join(OUTPUT_DIR, "_manifest.json")
    with open(manifest_path, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2)
    print(f"  _manifest.json")
    
    print(f"\nDone! All game asset data saved to {OUTPUT_DIR}")

if __name__ == "__main__":
    main()
