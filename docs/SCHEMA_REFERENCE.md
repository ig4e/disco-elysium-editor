# Disco Elysium Save Editor — Data Schema Reference

> **Audience**: An AI agent or developer building a C# WinUI save editor.  
> **Generated from**: Actual extracted game data + sample save file, February 2026.

---

## Table of Contents

1. [Overview & Architecture](#1-overview--architecture)
2. [File Inventory](#2-file-inventory)
3. [Save File Structure](#3-save-file-structure)
4. [Game Asset Schemas](#4-game-asset-schemas)
5. [ID Cross-Reference System](#5-id-cross-reference-system)
6. [Skill Key Mapping](#6-skill-key-mapping)
7. [Enums & Constants](#7-enums--constants)

---

## 1. Overview & Architecture

The save editor operates on two data sources:

| Source | Content | Format | Mutability |
|--------|---------|--------|------------|
| **Save folder** (`*.ntwtf/`) | Player's runtime state — skills, inventory, quests, variables | JSON + binary Lua + Lua script | **Read/Write** (the editor modifies these) |
| **Game assets** (`output/game_assets/`) | Static definitions — item descriptions, thought bonuses, skill names, quest text | Pre-extracted JSON | **Read-only** (reference data for display & validation) |

The save uses string IDs (e.g. `"jacket_suede"`, `"hobocop"`, `"TASK.sing_karaoke"`) that match the `name` fields in game asset files exactly. No ID translation is needed.

### Save Folder Contents

A save is a folder named `<SaveName>.ntwtf/` containing:

| File | Format | Purpose |
|------|--------|---------|
| `<SaveName>.1st.ntwtf.json` | JSON | Party state, current area, fog of war status cache |
| `<SaveName>.2nd.ntwtf.json` | JSON | Everything else: character, inventory, journal, thoughts, containers, HUD |
| `<SaveName>.FOW.json` | JSON | Fog of war grid data (empty `{}` until explored) |
| `<SaveName>.ntwtf.lua` | Binary TLV | PixelCrushers dialogue variable database (12,000+ runtime vars) |
| `<SaveName>.states.lua` | Lua text | Area progression states + shown interaction orbs |

**For the editor**, the `.1st` and `.2nd` JSON files are the primary targets. The `.ntwtf.lua` is a binary database of all dialogue variables and can also be edited. The `.states.lua` is a text Lua script.

---

## 2. File Inventory

### Game Asset Files (read-only reference, `output/game_assets/`)

| File | Count | Schema | Purpose |
|------|-------|--------|---------|
| `actors_skills.json` | 28 | `ActorSkill[]` | 4 attributes + 24 skills with descriptions |
| `actors_npcs_major.json` | 122 | `Actor[]` | Named NPCs (Cuno, Kim, Joyce...) |
| `actors_npcs_minor.json` | 263 | `Actor[]` | Unnamed/minor NPCs |
| `actors_player.json` | 1 | `Actor[]` | Player character ("You") |
| `actors_voices.json` | 10 | `Actor[]` | Brain voices (Ancient Reptilian Brain, etc.) |
| `items_inventory.json` | 206 | `Item[]` | All inventory items |
| `items_thoughts.json` | 53 | `Thought[]` | Thought Cabinet entries with bonuses |
| `conversations_index.json` | 1,501 | `Conversation[]` | Dialogue tree metadata |
| `variables_tasks.json` | 1,131 | `Variable[]` | Quest flags with descriptions |
| `variables_xp.json` | 689 | `VariableXP[]` | XP reward flags with point values |
| `variables_reputation.json` | 21 | `Variable[]` | Political alignment + cop archetype counters |
| `variables_character.json` | 111 | `Variable[]` | Character state flags |
| `variables_stats.json` | 41 | `Variable[]` | Stats tracking |
| `variables_inventory.json` | 266 | `Variable[]` | Inventory-related flags |
| `variables_locations.json` | 8,307 | `VariableLoc[]` | Location-specific state |
| `variables_auto.json` | 56 | `Variable[]` | Auto-triggered flags |
| `variables_globals.json` | 5 | `Variable[]` | Global state |
| `variables_all.json` | 10,645 | `VariableLoc[]` | Complete variable reference |
| `skill_key_map.json` | — | `SkillKeyMap` | Save key ↔ type ↔ display name mapping |
| `save_file_schema.json` | — | `Schema` | Full save file field documentation |
| `_manifest.json` | — | `Manifest` | Extraction metadata |

---

## 3. Save File Structure

### 3.1 First File (`.1st.ntwtf.json`)

```jsonc
{
  "areaId": "Martinaise-ext",              // string — current area
  "partyState": {
    "isKimInParty": true,                   // bool
    "isKimLeftOutside": false,              // bool
    "isKimAbandoned": false,                // bool
    "isKimAwayUpToMorning": false,          // bool
    "isKimSleepingInHisRoom": false,        // bool
    "isKimSayingGoodMorning": false,        // bool
    "isCunoInParty": false,                 // bool
    "isCunoLeftOutside": false,             // bool
    "isCunoAbandoned": true,                // bool
    "hasHangover": false,                   // bool — Day 1 hangover
    "sleepLocation": 0,                     // int
    "waitLocation": 0,                      // int
    "cunoWaitLocation": 0,                  // int
    "timeSinceKimWentSleepingInHisRoom": 0, // int — minutes
    "kimLastArrivalLocation": 0,            // int
    "cunoLastArrivalLocation": 0            // int
  },
  "fowUnrevealersStatusCache": {            // map<string, string>
    "fog-whirling-klaasje-main": "UNSEEN",  // values: "UNSEEN" | "ACTIVE" | "DONE"
    "fog-main": "ACTIVE"
  }
}
```

### 3.2 Second File (`.2nd.ntwtf.json`)

Top-level keys (13 sections):

```
variousItemsHolder, sunshineClockTimeHolder, characterSheet,
playerCharacter, hudState, aquiredJournalTasks, failedWhiteChecksHolder,
weatherState, inventoryState, thoughtCabinetState, containerSourceState,
kubujussState, gameModeState
```

#### 3.2.1 `playerCharacter`

```jsonc
{
  "XpAmount": 90,           // int — total XP earned
  "Level": 2,               // int — current level
  "SkillPoints": 1,         // int — unspent points
  "Money": 40,              // int — Réal in cents (40 = 0.40 Réal)
  "StockValue": 0,          // int — pawn stock value
  "NewPointsToSpend": true,  // bool — skill point notification
  "healingPools": {
    "ENDURANCE": 0,          // int — health pips (0 = dead)
    "VOLITION": 0            // int — morale pips (0 = dead)
  }
}
```

#### 3.2.2 `characterSheet`

Contains ability entries (4), skill entries (24), item/thought arrays, and modifier maps.

**Ability entry** (keys: `intellect`, `psyche`, `fysique`, `motorics`):
```jsonc
{
  "abilityType": "INT",      // enum: INT | PSY | FYS | MOT
  "dirty": false,            // bool — recalc needed
  "value": 5,                // int — current score
  "valueWithoutPerceptionsSubSkills": 5,  // int
  "damageValue": 0,          // int
  "maximumValue": 5,         // int — must >= value
  "calculatedAbility": 0,    // int — always 0 for abilities
  "rankValue": 0,            // int
  "hasAdvancement": false,   // bool
  "isSignature": false,      // bool — archetype signature
  "modifiers": null           // null or array
}
```

**Skill entry** (keys: `logic`, `encyclopedia`, etc. — see §6 for full mapping):
```jsonc
{
  "skillType": "LOGIC",      // enum — UPPER_CASE type code
  "abilityType": "INT",      // enum — parent ability
  "dirty": false,
  "value": 5,                // int — total (base + rank + modifiers)
  "valueWithoutPerceptionsSubSkills": 5,
  "damageValue": 0,
  "maximumValue": 5,         // int — learning cap
  "calculatedAbility": 5,    // int — base from parent ability
  "rankValue": 0,            // int — skill points invested
  "hasAdvancement": false,   // bool — can level up
  "isSignature": false,
  "modifiers": null
}
```

> **Skill value formula**: `value = calculatedAbility + rankValue + Σ(modifier amounts)`

**Collection fields**:
```jsonc
{
  "gainedItems": ["jacket_suede", "prybar", "flashlight"],     // string[] — item name IDs
  "equippedItems": ["jacket_suede", "prybar"],                 // string[] — subset of gainedItems
  "gainedThoughts": ["guillaume_le_million", "the_way_home"],  // string[] — thought name IDs
  "cookingThoughts": ["the_way_home"],                         // string[] — being researched
  "fixedThoughts": ["guillaume_le_million"],                   // string[] — internalized
  "forgottenThoughts": [],                                     // string[] — removed
  "selectedPanelName": ""                                       // string — UI state
}
```

**`SkillModifierCauseMap`** — `map<string, ModifierEntry[]>` keyed by skill type (e.g. `"LOGIC"`, `"COMPOSURE"`):
```jsonc
{
  "LOGIC": [
    {
      "type": "CALCULATED_ABILITY",              // see §7.4 for values
      "amount": 0,                               // int — bonus amount
      "explanation": "Logic base",               // string — display text
      "skillType": "LOGIC",                      // string
      "modifierCause": {
        "ModifierKey": "INT",                    // string — source ID
        "ModifierCauseType": "ABILITY"           // enum: ABILITY | THOUGHT | INVENTORY_ITEM
      }
    }
  ],
  "ENCYCLOPEDIA": [
    // ABILITY base entry (always present)
    { "type": "CALCULATED_ABILITY", "amount": 0, "explanation": "Encyclopedia base",
      "skillType": "ENCYCLOPEDIA", "modifierCause": { "ModifierKey": "INT", "ModifierCauseType": "ABILITY" } },
    // Thought bonus example
    { "type": "THC", "amount": 1, "explanation": "+1 Encyclopedia",
      "skillType": "ENCYCLOPEDIA", "modifierCause": { "ModifierKey": "the_way_home", "ModifierCauseType": "THOUGHT" } },
    // Item bonus example
    { "type": "INVENTORY_ITEM", "amount": 1, "explanation": "+1 Encyclopedia",
      "skillType": "ENCYCLOPEDIA", "modifierCause": { "ModifierKey": "jacket_suede", "ModifierCauseType": "INVENTORY_ITEM" } }
  ]
}
```

**`AbilityModifierCauseMap`** — `map<string, ModifierEntry[]>` keyed by ability type (`"INT"`, `"PSY"`, `"FYS"`, `"MOT"`):
```jsonc
{
  "INT": [
    {
      "type": "INITIAL_DICE",
      "amount": 5,
      "explanation": "Intellect base",
      "skillType": "NONE",
      "modifierCause": { "ModifierKey": "INT", "ModifierCauseType": "ABILITY" }
    }
  ]
}
```

#### 3.2.3 `sunshineClockTimeHolder`

```jsonc
{
  "time": {
    "dayCounter": 1,        // int — game day (1-based)
    "realDayCounter": 1,    // int
    "dayMinutes": 777,      // int — 0-1439. hours=dayMinutes÷60, mins=dayMinutes%60 (777 = 12:57)
    "seconds": 37           // int — 0-59
  },
  "timeOverride": null       // null or time object (scripted sequences)
}
```

#### 3.2.4 `aquiredJournalTasks`

```jsonc
{
  // When tasks were discovered — map<string, Timestamp>
  "TaskAquisitions": {
    "TASK.sing_karaoke": { "dayCounter": 1, "realDayCounter": 1, "dayMinutes": 508, "seconds": 54 },
    "TASK.talk_to_the_manager": { "dayCounter": 1, "realDayCounter": 1, "dayMinutes": 527, "seconds": 41 }
  },

  // When tasks were resolved — map<string, Timestamp|{}>  (empty {} = not resolved)
  "TaskResolutions": {
    "TASK.sing_karaoke": {},                              // not resolved yet
    "TASK.talk_to_the_manager": { "dayCounter": 1, "realDayCounter": 1, "dayMinutes": 546, "seconds": 28 }
  },

  // Subtasks — map<string, map<string, Timestamp>>
  "SubtaskAquisitions": {
    "TASK.sing_karaoke": {
      "TASK.find_song": { "dayCounter": 1, "realDayCounter": 1, "dayMinutes": 508, "seconds": 54 }
    }
  },

  // Read/new badges — map<string, bool>  (true = seen by player)
  "TaskNewStates": {
    "TASK.open_trash_container": true,
    "TASK.interview_wild_pines_rep": false       // false = has "NEW" badge
  },

  "ChecksWithNotifications": [],                // array — skill check notifications
  "LastActiveTask": "",                          // string — currently tracked
  "LastDoneTask": "",                            // string — most recent complete
  "TasksTabNotifyIcon": false,                   // bool — journal tab dots
  "ChecksTabNotifyIcon": false,
  "ActiveTasksTabNotifyIcon": false,
  "DoneTasksTabNotifyIcon": false,
  "HudNotifyIcon": true,                         // bool — HUD notification
  "wasChurchVisited": false,
  "wasFishingVillageVisited": false,
  "wasQuicktravelChurchDiscovered": false,
  "wasQuicktravelFishingVillageDiscovered": false
}
```

> **Important**: Task keys are `TASK.*` variable names. Look them up in `variables_tasks.json` to get human-readable descriptions.

#### 3.2.5 `inventoryState`

```jsonc
{
  "itemListState": [                          // Array of ALL 206 item states
    {
      "itemName": "key_trash_container",      // string — matches items_inventory.json "name"
      "isFresh": true,                        // bool — "NEW" badge
      "substanceUses": 0,                     // int — remaining consumable uses
      "substanceTimeLeft": 0,                 // int — active effect timer
      "StackItems": null                      // null or array (for stackable items like money/tare)
    }
  ],
  "inventoryViewState": {
    "equipment": {                            // map<SlotName, ItemName>
      "PANTS": "pants_bellbottom",
      "JACKET": "jacket_suede",
      "SHIRT": "shirt_dress_disco",
      "NECK": "neck_tie",
      "SHOES": "shoes_snakeskin",
      "HELDLEFT": "prybar",
      "HELDRIGHT": "chaincutters",
      "GLOVES": "gloves_garden"
      // Also: "HAT", "GLASSES" when equipped
    },
    "inventory": {                            // map<Category, items>
      "TOOLS": [{"Key": 2, "Value": "flashlight"}],
      "CLOTHES": [],
      "PAWNABLES": [],
      "READING": []
    },
    "bullets": 0,                             // int
    "keys": ["Key to Room #1"],               // string[] — key ring display names
    "lastSelectedItem": "chaincutters"        // string
  },
  "wearingBodysuit": false                     // bool — hides clothing when true
}
```

#### 3.2.6 `thoughtCabinetState`

```jsonc
{
  "thoughtListState": [                       // Array of ALL 53 thoughts
    {
      "name": "radical_feminist_agenda",      // string — matches items_thoughts.json "name"
      "isFresh": false,                       // bool — "NEW" badge
      "state": "COOKING",                     // enum: UNKNOWN | COOKING | FIXED | FORGOTTEN
      "timeLeft": 224                         // float — minutes remaining (0 when fixed)
    },
    {
      "name": "guillaume_le_million",
      "isFresh": false,
      "state": "FIXED",
      "timeLeft": 0
    }
  ],
  "thoughtCabinetViewState": {
    "slotStates": [                           // Array of cabinet slots
      { "Item1": "FILLED", "Item2": "guillaume_le_million" },   // slot states: FILLED | BUYABLE | LOCKED
      { "Item1": "BUYABLE", "Item2": null },
      { "Item1": "LOCKED", "Item2": null }
    ],
    "selectedProjectName": ""                 // string — currently selected in UI
  }
}
```

#### 3.2.7 `containerSourceState`

```jsonc
{
  "itemRegistry": {                           // map<string, ContainerItem[]> — 402 containers
    "Interactable whirl f2/Shelves11": [      // key = Unity scene object path
      {
        "name": "pants_rcm",                  // string — item name ID
        "probability": 1,                     // float — spawn probability
        "value": 0,                           // int
        "deviation": 0,                       // int
        "calculatedValue": 0,                 // int
        "bonusLoot": false                    // bool
      }
    ]
  }
}
```

#### 3.2.8 `failedWhiteChecksHolder`

```jsonc
{
  "ReopenedWhiteChecksByActorName": {},       // map — checks reopened after skill increase
  "WhiteCheckCache": {},                      // map — failed checks eligible for retry
  "SeenWhiteCheckCache": {                    // map<string, WhiteCheck> — all checks encountered
    "whirling.mirror_subdued_expression": {   // key = location variable name
      "FlagName": "whirling.mirror_subdued_expression",
      "SkillType": "ELECTROCHEMISTRY",        // string — the skill being tested
      "LastSkillValue": 0,                    // int — player's skill when last attempted
      "LastTargetValue": 16,                  // int — difficulty target
      "difficulty": 18,                       // int — base difficulty
      "isOnlySeen": true,                     // bool — seen but not attempted
      "checkPrecondition": "Variable[\"whirling.mirror_endurance_rc\"] == false",
      "checkTargetArticyId": "0x0000000000000000",
      "Actor": { /* full PixelCrushers actor object */ },
      "CheckModifiers": {                     // map<string, CheckModifier[]>
        "whirling.mirror_expression_source_located": [
          {
            "expression": "Variable[\"whirling.mirror_expression_source_located\"]",
            "bonus": -2,                      // negative = easier
            "fallbackExplanation": "Know the origin."
          }
        ]
      }
    }
  },
  "ChecksBySkill": {},                        // map — checks grouped by skill type
  "ChecksByVariable": {}                      // map — checks by variable
}
```

#### 3.2.9 Other Sections

**`variousItemsHolder`**:
```jsonc
{
  "Obsessions": ["WHIRLING F2 ORB / dialogue pants"],  // string[] — seen interaction orbs
  "DoorStates": { "Whirling Door Tequila": false },    // map<string, bool>
  "BuildNumber": "02664d27"                             // string — game build hash
}
```

**`hudState`**:
```jsonc
{
  "tequilaPortraitObscured": false,    // bool — portrait in shadow
  "tequilaPortraitShaved": false,      // bool — shaved face
  "tequilaPortraitExpressionStopped": false,
  "tequilaPortraitFascist": false,     // bool — fascist hairstyle
  "charsheetNotification": true,       // bool — tab notification dots
  "inventoryNotification": false,
  "journalNotification": true,
  "thcNotification": false,            // Thought Cabinet
  "invClothesNotification": false,
  "invPawnablesNotification": false,
  "invReadingNotification": false,
  "invToolsNotification": false
}
```

**`weatherState`**: `{ "weatherPreset": 9 }` — int, preset index.

**`gameModeState`**: `{ "gameMode": "NORMAL", "wasSwitched": false }` — values: `"NORMAL"` | `"HARDCORE"`.

**`kubujussState`**:
```jsonc
{
  "CutsceneObjectStates": [
    { "name": "molotov", "lastAnimationHash": -1443730330, "triggeredFunctions": "", "parameters": {} }
  ]
}
```

### 3.3 States File (`.states.lua`)

Plain text Lua with two assignment patterns:

```lua
AreaState["CHURCH_AUXWIRE"]={LocationState=0};
AreaState["DOOMED_DOOR"]={LocationState=0};
-- ... 138 total entries

ShownOrbs["WHIRLING F2 ORB / ashtray"]={OrbSeen=1};
ShownOrbs["WHIRLING F1 ORB / karaoke mic"]={OrbSeen=1};
-- ... N entries (grows as player explores)
```

Parse with regex: `AreaState\["(.+?)"\]\s*=\s*\{LocationState=(\d+)\}` and `ShownOrbs\["(.+?)"\]\s*=\s*\{OrbSeen=(\d+)\}`.

### 3.4 FOW File (`.FOW.json`)

Empty `{}` until fog is revealed. Structure is map-specific grid data when populated.

### 3.5 Binary Lua Database (`.ntwtf.lua`)

Binary TLV format containing 12,000+ runtime dialogue variables. **This is the most complex file format**: see the Go parser source at `scraper/parser/lua_parser.go` for a complete implementation.

| Type Byte | Name | Encoding |
|-----------|------|----------|
| `0x53` (`S`) | String | 7-bit encoded length + UTF-8 bytes |
| `0x4E` (`N`) | Number | 8-byte LE IEEE 754 float64 |
| `0x42` (`B`) | Boolean | Single byte (0/1) |
| `0x54` (`T`) | Table | 4-byte padding + 4-byte LE entry count + N key-value pairs |

The 7-bit length encoding matches .NET's `BinaryWriter.Write7BitEncodedInt` — read bytes, shift the low 7 bits into position, stop when the high bit is 0.

---

## 4. Game Asset Schemas

### 4.1 `Actor` (NPCs, player, voices)

```jsonc
{
  "id": 5,                                    // int — PixelCrushers internal ID
  "name": "Cuno",                             // string — unique identifier
  "display_name": "Cuno",                     // string — display text
  "articy_id": "0x010000000000004FC",          // string — Articy:Draft cross-ref ID
  "short_name": "cuno",                       // string — lowercase reference key
  "description": "",                          // string — short description
  "long_description": "",                     // string — extended bio
  "portrait": "[portrait_cuno.png]",          // string — portrait asset reference
  "is_female": false,                         // bool|string
  "category": "npc_major"                     // enum: npc_major | npc_minor | player | voice
}
```

### 4.2 `ActorSkill` (extends Actor)

```jsonc
{
  // ... all Actor fields ...
  "description": "Wield raw intellectual power. Deduce the world.",
  "long_description": "COOL FOR: ANALYSTS. PURE RATIONALISTS...",   // full flavor text
  "category": "skill",
  "attribute_group": "INT",                   // enum: INT | PSY | FYS | MOT
  "is_attribute": false                       // bool — true for the 4 parent attributes
}
```

### 4.3 `Item`

```jsonc
{
  "id": 107,
  "name": "jacket_suede",                    // string — THE ID used in save files
  "display_name": "jacket_suede",
  "articy_id": "0x01000017000019E4",
  "short_name": "jacket_suede",
  "description": "Looks like someone skinned this blazer off some long extinct disco-animal...",
  "category": "item",
  "item_type": 3.0,                          // float — item type enum (see §7.5)
  "item_group": 0.0,                         // float — item group enum
  "item_value": 0.0,                         // float — sell value
  "equip_slot": "",                          // string
  "bonus": "",                               // string — bonus description
  "skill_modifier": "",                      // string
  "is_quest_item": false,                    // bool
  "MediumTextValue": "+1 Esprit de Corps: Halogen watermarks",  // string — item tooltip with bonuses
  "autoequip": "True",                       // string(bool) — auto-equip on pickup
  "cursed": "False",                         // string(bool) — cannot unequip
  "isSubstance": "False",                    // string(bool) — drug/alcohol
  "isConsumable": "False",                   // string(bool)
  "multipleAllowed": "False"                 // string(bool) — can hold multiple
}
```

> **Note**: Some boolean fields are stored as strings `"True"`/`"False"` — this comes from the PixelCrushers field system. Parse with case-insensitive string comparison.

### 4.4 `Thought`

```jsonc
{
  "id": 4,
  "name": "hobocop",                         // string — THE ID used in save files
  "display_name": "hobocop",
  "articy_id": "0x0100001700005193",
  "short_name": "hobocop",
  "description": "A cop -- and a hobo. A hobo cop. Upsides: can be disheveled...",
  "category": "thought",
  "thought_type": "other",                   // enum: INT | PSY | MOT | FYS | other
  "thought_type_raw": 5.0,                   // float — raw type enum value
  "bonus_while_processing": "-1 Composure: Ungainly and rambling",           // string
  "bonus_when_completed": "Reveals extra special collector's edition tare bottles on the map\nMore money from tare!",  // string (can be multi-line)
  "completion_description": "A cop -- and a hobo...",                         // string
  "time_to_internalize": 180.0,              // float — minutes to research
  "requirement": "Earn money through Can and Bottle Return",                 // string — unlock condition
  "is_cursed": false                         // bool — cannot remove once started
}
```

### 4.5 `Variable`

```jsonc
{
  "id": 5789,
  "name": "TASK.talk_to_the_manager",        // string — THE ID used in save files
  "initial_value": "False",                  // string — can be "False", "True", "0", "0.0", etc.
  "description": "Talk to the manager who runs the Whirling-in-Rags."
}
```

### 4.6 `VariableXP` (extends Variable)

```jsonc
{
  "id": 9320,
  "name": "XP.assess_your_medical_condition",
  "initial_value": "False",
  "description": "50",                       // NOTE: description contains the XP point value
  "xp_points": "50"                          // string — extracted XP value
}
```

### 4.7 `VariableLoc` (extends Variable — has location prefix)

```jsonc
{
  "id": 1,
  "name": "apt.orb_viscal_pleasure_wheel_intro_done",
  "initial_value": "False",
  "description": "",
  "location": "apt"                          // string — the prefix before the first dot
}
```

### 4.8 `Conversation`

```jsonc
{
  "id": 1,
  "title": "Disco Elysium: Final Cut",
  "description": "",
  "articy_id": "0x0000000000000000",
  "num_entries": 2,                          // int — dialogue entry count
  "num_skill_checks": 0,                     // int — skill checks in this conversation
  "actor_ids": []                            // int[] — actor IDs involved
}
```

---

## 5. ID Cross-Reference System

The editor needs to look up game definitions when displaying save data. Here's how IDs connect:

### 5.1 Items

| Save Location | ID Field | Lookup Target |
|---------------|----------|---------------|
| `characterSheet.gainedItems[]` | string value | `items_inventory.json` → `name` |
| `characterSheet.equippedItems[]` | string value | `items_inventory.json` → `name` |
| `inventoryState.itemListState[].itemName` | string | `items_inventory.json` → `name` |
| `inventoryState.inventoryViewState.equipment.*` | string value | `items_inventory.json` → `name` |
| `containerSourceState.itemRegistry.*[].name` | string | `items_inventory.json` → `name` |
| `SkillModifierCauseMap.*[].modifierCause.ModifierKey` (when `ModifierCauseType == "INVENTORY_ITEM"`) | string | `items_inventory.json` → `name` |

### 5.2 Thoughts

| Save Location | ID Field | Lookup Target |
|---------------|----------|---------------|
| `characterSheet.gainedThoughts[]` | string value | `items_thoughts.json` → `name` |
| `characterSheet.cookingThoughts[]` | string value | `items_thoughts.json` → `name` |
| `characterSheet.fixedThoughts[]` | string value | `items_thoughts.json` → `name` |
| `characterSheet.forgottenThoughts[]` | string value | `items_thoughts.json` → `name` |
| `thoughtCabinetState.thoughtListState[].name` | string | `items_thoughts.json` → `name` |
| `SkillModifierCauseMap.*[].modifierCause.ModifierKey` (when `ModifierCauseType == "THOUGHT"`) | string | `items_thoughts.json` → `name` |

### 5.3 Tasks/Quests

| Save Location | ID Field | Lookup Target |
|---------------|----------|---------------|
| `aquiredJournalTasks.TaskAquisitions.*` | key | `variables_tasks.json` → `name` |
| `aquiredJournalTasks.TaskResolutions.*` | key | `variables_tasks.json` → `name` |
| `aquiredJournalTasks.SubtaskAquisitions.*` | key + value keys | `variables_tasks.json` → `name` |
| `aquiredJournalTasks.TaskNewStates.*` | key | `variables_tasks.json` → `name` |

### 5.4 Skills

| Save Location | ID Field | Lookup Target |
|---------------|----------|---------------|
| `characterSheet.<camelCaseKey>` | object key | `skill_key_map.json` → `save_key` → `display_name` |
| `SkillModifierCauseMap.<UPPER_TYPE>` | key | `skill_key_map.json` → `skill_type` → `display_name` |
| `failedWhiteChecksHolder.*.SkillType` | string | `skill_key_map.json` → `skill_type` |

### 5.5 Reputation / Variables

| Save Location | ID Field | Lookup Target |
|---------------|----------|---------------|
| `.ntwtf.lua` database variables | variable name | `variables_all.json` → `name` |
| `failedWhiteChecksHolder.SeenWhiteCheckCache.*` | key | `variables_locations.json` → `name` |

---

## 6. Skill Key Mapping

This is critical. The save file uses **three different naming conventions** for the same skill:

| Save Key (camelCase) | Skill Type (UPPER_CASE) | Display Name | Ability |
|----------------------|------------------------|--------------|---------|
| `intellect` | `INT` | Intellect | INT |
| `psyche` | `PSY` | Psyche | PSY |
| `fysique` | `FYS` | Fysique | FYS |
| `motorics` | `MOT` | Motorics | MOT |
| `logic` | `LOGIC` | Logic | INT |
| `encyclopedia` | `ENCYCLOPEDIA` | Encyclopedia | INT |
| `rhetoric` | `RHETORIC` | Rhetoric | INT |
| `drama` | `DRAMA` | Drama | INT |
| `conceptualization` | `CONCEPTUALIZATION` | Conceptualization | INT |
| `visualCalculus` | `VISUAL_CALCULUS` | Visual Calculus | INT |
| `volition` | `VOLITION` | Volition | PSY |
| `inlandEmpire` | `INLAND_EMPIRE` | Inland Empire | PSY |
| `empathy` | `EMPATHY` | Empathy | PSY |
| `authority` | `AUTHORITY` | Authority | PSY |
| `suggestion` | `SUGGESTION` | Suggestion | PSY |
| `espritDeCorps` | `ESPRIT_DE_CORPS` | Esprit de Corps | PSY |
| `endurance` | `ENDURANCE` | Endurance | FYS |
| `painThreshold` | `PAIN_THRESHOLD` | Pain Threshold | FYS |
| `physicalInstrument` | `PHYSICAL_INSTRUMENT` | Physical Instrument | FYS |
| `shivers` | `SHIVERS` | Shivers | FYS |
| `electrochemistry` | `ELECTROCHEMISTRY` | Electrochemistry | FYS |
| `halfLight` | `HALF_LIGHT` | Half Light | FYS |
| `handEyeCoordination` | `HE_COORDINATION` | Hand/Eye Coordination | MOT |
| `perception` | `PERCEPTION` | Perception | MOT |
| `reaction` | `REACTION` | Reaction Speed | MOT |
| `savoirFaire` | `SAVOIR_FAIRE` | Savoir Faire | MOT |
| `interfacing` | `INTERFACING` | Interfacing | MOT |
| `composure` | `COMPOSURE` | Composure | MOT |

> **Warning**: `reaction` → `REACTION` → "Reaction Speed" (display name differs from key) and `handEyeCoordination` → `HE_COORDINATION` (abbreviated in type code). These are the only two non-trivial mappings.

---

## 7. Enums & Constants

### 7.1 Ability Types
`INT` (Intellect), `PSY` (Psyche), `FYS` (Fysique), `MOT` (Motorics)

### 7.2 Equipment Slots
`PANTS`, `JACKET`, `SHIRT`, `NECK`, `SHOES`, `HELDLEFT`, `HELDRIGHT`, `GLOVES`, `HAT`, `GLASSES`

### 7.3 Inventory Categories
`TOOLS`, `CLOTHES`, `PAWNABLES`, `READING`

### 7.4 Modifier Types
- `CALCULATED_ABILITY` — Base score from parent ability
- `INITIAL_DICE` — Initial ability roll (archetype selection)
- `THC` — Thought Cabinet bonus
- `INVENTORY_ITEM` — Equipped item bonus

### 7.5 Modifier Cause Types  
- `ABILITY` — From parent ability score
- `THOUGHT` — From an internalized thought (ModifierKey = thought name)
- `INVENTORY_ITEM` — From an equipped item (ModifierKey = item name)

### 7.6 Thought States
`UNKNOWN` → `COOKING` → `FIXED` (or `FORGOTTEN`)

### 7.7 Thought Cabinet Slot States
`LOCKED`, `BUYABLE`, `FILLED`

### 7.8 Thought Types (attribute affinity)
`1.0` = INT, `2.0` = PSY, `3.0` = MOT, `4.0` = FYS, `5.0` = other

### 7.9 Fog of War Status
`UNSEEN`, `ACTIVE`, `DONE`

### 7.10 Game Modes
`NORMAL`, `HARDCORE`

### 7.11 Healing Pool Types
`ENDURANCE` (health), `VOLITION` (morale)

### 7.12 Political Alignment Variables
| Variable | Description |
|----------|-------------|
| `reputation.communist` | Communist counter (float) |
| `reputation.ultraliberal` | Ultraliberal counter (float) |
| `reputation.moralist` | Moralist counter (float) |
| `reputation.revacholian_nationhood` | Fascist/nationalist counter (float) |

### 7.13 Cop Archetype Variables
| Variable | Description |
|----------|-------------|
| `reputation.sorry_cop` | Sorry cop |
| `reputation.superstar_cop` | Superstar cop |
| `reputation.apocalypse_cop` | Apocalypse cop |
| `reputation.boring_cop` | Boring cop |
| `reputation.art_cop` | Art cop |
| `reputation.kim` | Kim Kitsuragi affection (float) |
