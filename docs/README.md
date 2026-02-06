# Disco Elysium Save Editor Toolkit

A comprehensive toolkit that extracts **all game data** from Disco Elysium — both the **runtime state** from save files and the **static game definitions** from Unity asset bundles — producing clean, categorized JSON files ready for building a save file editor.

## Architecture

The save file (`.ntwtf.lua`) stores **runtime variable values** — which quests are done, what items you have, current skill levels, reputation counters. But it doesn't contain **definitions** — item descriptions, thought bonuses, skill names, dialogue trees. Those live in Unity asset bundles inside `disco_Data/`.

This toolkit has two extraction pipelines:

| Pipeline | Language | Source | What it extracts |
|----------|----------|--------|-----------------|
| **Save File Parser** | Go | `.ntwtf.lua` binary TLV | Runtime state: 12,000+ variable values |
| **Game Asset Extractor** | Python | Unity asset bundles | Static definitions: actors, items, thoughts, conversations |

Together they give you everything needed for a complete save editor.

## Quick Start

### 1. Extract Game Definitions (one-time)

```bash
# Requires Python 3.10+ with UnityPy
pip install UnityPy
python scraper/extract_game_data.py
```

This reads the PixelCrushers DialogueDatabase from the dialogue bundle and produces 19 categorized JSON files in `output/game_assets/`.

### 2. Parse a Save File

```bash
cd scraper
go run . "<path-to-save-folder.ntwtf>"
# OR point directly at the .lua file:
go run . "<path-to-file.ntwtf.lua>"
```

Output lands in `output/`.

## Project Structure

```
Disco Elysium/
├── scraper/
│   ├── main.go                     # Save file CLI entry point
│   ├── go.mod
│   ├── parser/
│   │   └── lua_parser.go           # Binary TLV format reader
│   ├── classifier/
│   │   └── classifier.go           # Entity classification engine
│   ├── cleaner/
│   │   └── cleaner.go              # Text sanitization
│   ├── extract_game_data.py        # Game asset extractor (Python)
│   ├── extract_dialogue.py         # Raw dialogue DB dump (Python)
│   └── extract_assets.py           # General bundle explorer (Python)
│
├── output/
│   ├── game_assets/                # Static game definitions
│   │   ├── actors_skills.json      # 28 skills + attributes
│   │   ├── actors_npcs_major.json  # 122 major named NPCs
│   │   ├── actors_npcs_minor.json  # 263 minor NPCs
│   │   ├── actors_player.json      # Player character
│   │   ├── actors_voices.json      # 10 brain voices
│   │   ├── items_inventory.json    # 206 inventory items
│   │   ├── items_thoughts.json     # 53 thoughts with bonuses
│   │   ├── conversations_index.json# 1,501 conversations (112,962 entries)
│   │   ├── variables_tasks.json    # 1,131 quest variables + descriptions
│   │   ├── variables_xp.json       # 689 XP reward variables
│   │   ├── variables_reputation.json # 21 political alignment counters
│   │   ├── variables_character.json  # 111 character state vars
│   │   ├── variables_stats.json    # 41 stats variables
│   │   ├── variables_inventory.json  # 266 inventory state vars
│   │   ├── variables_locations.json  # 8,307 location-specific vars
│   │   ├── variables_all.json      # All 10,645 variables
│   │   ├── skill_key_map.json      # Save key ↔ type ↔ display name mapping
│   │   ├── save_file_schema.json   # Complete save file field documentation
│   │   └── _manifest.json          # Extraction summary
│   │
│   ├── items.json                  # Save file: runtime item state
│   ├── npcs.json                   # Save file: runtime NPC state
│   ├── thoughts.json               # Save file: runtime thought state
│   ├── skills.json                 # Save file: runtime skill values
│   ├── world_objects.json          # Save file: interactable objects
│   ├── task_variables.json         # Save file: quest progress flags
│   └── all_variables.json          # Save file: all runtime variables
│
├── examples/                       # Sample save files
├── docs/
│   ├── SCHEMA_REFERENCE.md           # Complete data schema for all output files
│   ├── SAVE_EDITOR_GUIDE.md          # How to use extracted data for editing saves
│   ├── WINUI_IMPLEMENTATION_GUIDE.md # C# WinUI implementation guide with model classes
│   └── ASSET_EXTRACTION_RESEARCH.md  # Unity asset extraction research notes
└── disco_Data/                     # Game install (asset bundles here)
```

## Extracted Game Data

### From Asset Bundles (static definitions)

| Category | Count | Description |
|----------|-------|-------------|
| **Skills** | 28 | 4 attributes (INT/PSY/FYS/MOT) + 24 skills with descriptions |
| **Major NPCs** | 122 | Named characters (Cuno, Kim Kitsuragi, Joyce, etc.) |
| **Minor NPCs** | 263 | Unnamed/minor characters |
| **Brain Voices** | 10 | Ancient Reptilian Brain, Limbic System, Perception senses |
| **Items** | 206 | Equipment, keys, consumables, evidence, books |
| **Thoughts** | 53 | Full Thought Cabinet: bonuses, requirements, internalization time |
| **Conversations** | 1,501 | Dialogue trees with 112,962 total entries |
| **Task Variables** | 1,131 | Quest flags with human-readable descriptions |
| **XP Variables** | 689 | XP rewards with point values |
| **Reputation** | 21 | Political alignment (communist, ultraliberal, moralist, fascist), cop archetypes, Kim affection |
| **Total Variables** | 10,645 | Complete game variable reference |

### From Save Files (runtime state)

| Category | Count* | Description |
|----------|--------|-------------|
| **Items** | ~206 | Current inventory with equipped state |
| **NPCs** | ~127 | Active NPC state and relationships |
| **Thoughts** | 53 | Thought Cabinet progress |
| **Skills** | 30 | Current skill levels |
| **World Objects** | ~267 | Interactable object state |
| **Variables** | ~12,000 | All runtime variable values |

*Counts from an early-game save.

## Key Data Relationships

A save editor needs both pipelines. Example workflow:

1. **Game assets** tell you a thought exists: `"hobocop"` with bonus `"+1 Composure"` and internalization time `180h`
2. **Save file** tells you whether the player has it: variable `character.thought_hobocop = true`
3. **Editor** can display the thought name/description/bonuses (from assets) alongside the current state (from save) and let the user toggle it

Same pattern for items, quests, reputation, skills.

## Technical Details

### Save File Format (.ntwtf.lua)

Binary TLV (Type-Length-Value) serialization:

| Type Byte | Name | Encoding |
|-----------|------|----------|
| `S` (0x53) | String | 7-bit encoded length + UTF-8 bytes |
| `N` (0x4E) | Number | 8-byte little-endian IEEE 754 float64 |
| `B` (0x42) | Boolean | Single byte (0 = false, 1 = true) |
| `T` (0x54) | Table | 4-byte padding + 4-byte LE count + N key-value pairs |

The 7-bit length encoding matches .NET's `BinaryWriter.Write7BitEncodedInt`.

### Game Asset Architecture

- **Engine**: Unity 2020.3.12f1 (IL2CPP build)
- **Dialogue Framework**: PixelCrushers Dialogue System for Unity
- **Asset Format**: Unity Addressables with UnityFS bundles
- **Key Bundle**: `dialoguebundle_assets_all_*.bundle` (10.9 MB) — complete `DialogueDatabase` ScriptableObject
- **Catalog**: `disco_Data/StreamingAssets/aa/catalog.json` indexes 51,379 assets across 1,827 bundles (3.8 GB)
- **Localization**: 12 languages in `disco_Data/StreamingAssets/AssetBundles/Windows/lockits/`

### Actor Classification (color field)

| Color Value | Category | Count | Examples |
|-------------|----------|-------|----------|
| 1.0 | Named NPCs | 122 | Cuno, Kim Kitsuragi, Joyce Messier |
| 2.0 | INT Skills | 7 | Intellect, Logic, Encyclopedia, Rhetoric, Drama, Conceptualization, Visual Calculus |
| 3.0 | PSY Skills | 7 | Psyche, Volition, Inland Empire, Empathy, Authority, Esprit de Corps, Suggestion |
| 4.0 | FYS Skills | 7 | Fysique, Endurance, Pain Threshold, Physical Instrument, Shivers, Electrochemistry, Half Light |
| 5.0 | MOT + Voices | 11 | Motorics, Hand/Eye, Savoir Faire, Ancient Reptilian Brain, Limbic System |
| 7.0 | Player | 1 | "You" |
| _(none)_ | Minor NPCs | 263 | Unnamed characters |
