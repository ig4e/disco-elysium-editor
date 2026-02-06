# Save Editor Guide — Using Extracted Data

This document explains how each extracted JSON file maps to the Disco Elysium save file format, and how to use them when building a save editor.

---

## Save File Structure

A save is a `.ntwtf` **folder** containing:

| File Pattern | Encoding | Purpose |
|---|---|---|
| `*.1st.ntwtf.json` | JSON | Party state, area, fog of war |
| `*.2nd.ntwtf.json` | JSON | Character sheet, inventory, thoughts, tasks, containers |
| `*.FOW.json` | JSON | Raw fog of war data |
| `*.ntwtf.lua` | Binary TLV | PixelCrushers dialogue variable database (12,000+ runtime vars) |
| `*.states.lua` | Lua script | Area states, object visibility, world flags |

The **JSON files** are directly editable. The **`.ntwtf.lua`** is a binary TLV database storing all 12,000+ PixelCrushers dialogue variables at runtime — it can also be edited but requires binary parsing (see Go parser at `scraper/parser/lua_parser.go`). The **reference catalog** for valid values comes from the pre-extracted game asset files in `output/game_assets/`.

---

## 1. Character Stats (`.2nd.ntwtf.json`)

### Abilities (INT, PSY, FYS, MOT)

Path: `characterSheet.<ability>`

```json
{
  "characterSheet": {
    "intellect": {
      "abilityType": "INT",
      "value": 5,            // ← EDIT THIS: current ability score
      "maximumValue": 5,     // ← EDIT THIS: must match or exceed value
      "isSignature": false    // ← true if this was the archetype's signature
    }
  }
}
```

| Ability Key | Type Code | Skills Governed |
|---|---|---|
| `intellect` | `INT` | logic, encyclopedia, rhetoric, drama, conceptualization, visualCalculus |
| `psyche` | `PSY` | volition, inlandEmpire, empathy, authority, suggestion, espritDeCorps |
| `fysique` | `FYS` | physicalInstrument, electrochemistry, endurance, halfLight, painThreshold, shivers |
| `motorics` | `MOT` | handEyeCoordination, perception, reaction, savoirFaire, interfacing, composure |

### Skills (24 total)

Path: `characterSheet.<skillName>`

```json
{
  "characterSheet": {
    "logic": {
      "skillType": "LOGIC",
      "abilityType": "INT",
      "dirty": false,            // ← Recalculation flag
      "value": 5,              // ← Current skill value (base + modifiers)
      "valueWithoutPerceptionsSubSkills": 5,  // ← Value without perception bonuses
      "damageValue": 0,        // ← Damage/penalty applied
      "maximumValue": 5,       // ← Learning cap
      "calculatedAbility": 5,  // ← Base from parent ability
      "rankValue": 0,          // ← Skill points invested
      "hasAdvancement": false,  // ← Can be leveled up
      "isSignature": false,
      "modifiers": null         // ← Active modifiers (null or array)
    }
  }
}
```

**Skill value formula:** `value = calculatedAbility + rankValue + sum(modifiers)`

When editing, update `value` and `maximumValue` together. Setting `maximumValue` higher than `value` lets the player level it up.

### Player Resources

Path: `playerCharacter.*`

```json
{
  "playerCharacter": {
    "XpAmount": 90,        // Total XP earned
    "Level": 2,            // Current level
    "SkillPoints": 1,      // Unspent skill points
    "Money": 40            // Réal (currency, in cents)
  }
}
```

### Health & Morale

Path: `playerCharacter.healingPools.*`

```json
{
  "playerCharacter": {
    "healingPools": {
      "ENDURANCE": 3,    // Health pips (die at 0)
      "VOLITION": 3      // Morale pips (die at 0)
    }
  }
}
```

---

## 2. Inventory (`.2nd.ntwtf.json`)

### Adding/Removing Items

Path: `characterSheet.gainedItems[]` and `characterSheet.equippedItems[]`

Use **`output/game_assets/items_inventory.json`** to find valid item IDs. The `name` field is the item ID used in the save.

```json
{
  "characterSheet": {
    "gainedItems": [
      "pants_bellbottom",     // Item IDs from items.json
      "jacket_suede",
      "flashlight",
      "prybar"
    ],
    "equippedItems": [
      "pants_bellbottom",     // Subset of gainedItems
      "jacket_suede"
    ]
  }
}
```

**To add an item:**
1. Look up the item in `output/game_assets/items_inventory.json` by `name` (e.g., `"drug_alcohol_commodore_red"`)
2. Add it to `gainedItems[]`
3. Optionally add to `equippedItems[]` if it should be worn
4. If item has skill bonuses (see `MediumTextValue` field), add entries to `SkillModifierCauseMap`

**To remove an item:**
1. Remove from both `gainedItems[]` and `equippedItems[]`
2. Also remove any related `SkillModifierCauseMap` entries with that item as `ModifierKey`

### Equipment Slots

Items have an `itemGroup` that determines their slot:

| itemGroup Value | Slot |
|---|---|
| `equipOrb` | Default / miscellaneous |
| Equipment slot is determined by item type in the game engine |

### Skill Modifier Map

Path: `characterSheet.SkillModifierCauseMap.<SKILL_TYPE>[]`

Every bonus/penalty to a skill is tracked here. When adding items with skill bonuses, you must add entries:

```json
{
  "SkillModifierCauseMap": {
    "ENCYCLOPEDIA": [
      {
        "type": "CALCULATED_ABILITY",
        "amount": 0,
        "explanation": "Encyclopedia base",
        "skillType": "ENCYCLOPEDIA",
        "modifierCause": {
          "ModifierKey": "INT",
          "ModifierCauseType": "ABILITY"
        }
      },
      {
        "type": "THC",
        "amount": 1,
        "explanation": "+1 Encyclopedia",
        "skillType": "ENCYCLOPEDIA",
        "modifierCause": {
          "ModifierKey": "the_way_home",      // Thought or item ID
          "ModifierCauseType": "THOUGHT"       // ABILITY, THOUGHT, or INVENTORY_ITEM
        }
      }
    ]
  }
}
```

**ModifierCauseType values:**
- `"ABILITY"` — Base stat from parent ability
- `"THOUGHT"` — Bonus from a Thought Cabinet thought
- `"INVENTORY_ITEM"` — Bonus from an equipped item

---

## 3. Thought Cabinet (`.2nd.ntwtf.json`)

### Reference: `output/game_assets/items_thoughts.json`

Each thought has these states in the save:

Path: `characterSheet.*Thoughts[]` and `thoughtCabinetState.*`

```json
{
  "characterSheet": {
    "gainedThoughts": ["hobocop", "the_way_home"],     // Discovered
    "cookingThoughts": ["the_way_home"],                // Currently researching
    "fixedThoughts": ["hobocop"],                       // Fully internalized
    "forgottenThoughts": []                              // Removed from cabinet
  }
}
```

**Thought lifecycle:** discovered → cooking → fixed (or forgotten)

The `items_thoughts.json` output gives you:
- `description` — Research text shown while cooking
- `fixtureDescription` — Final text after internalization
- `bonus` — Active bonus (e.g., "+1 Encyclopedia")
- `fixtureBonus` — Bonus after full internalization
- `requirement` — How to unlock the thought

**To add a thought:**
1. Find the thought by `name` in `output/game_assets/items_thoughts.json`
2. Add to `gainedThoughts[]`
3. Add to `fixedThoughts[]` for instant internalization, or `cookingThoughts[]` to research
4. Update `thoughtCabinetState.thoughtListState[]` entry (set `state` to `"FIXED"` or `"COOKING"`, `timeLeft` accordingly)
5. Update `thoughtCabinetState.thoughtCabinetViewState.slotStates[]` (set slot to `{"Item1": "FILLED", "Item2": "thought_name"}`)
6. Add corresponding entries to `SkillModifierCauseMap` for skill bonuses

---

## 4. Tasks & Quests (`.2nd.ntwtf.json`)

### Reference: `output/game_assets/variables_tasks.json`

Path: `aquiredJournalTasks.*`

The journal tracks task acquisition, completion, and subtask states. The extracted task variables in `variables_tasks.json` provide human-readable descriptions for all 1,131 task flags (e.g., `TASK.whirling_manager_talk_done` → "Talk to the manager who runs the Whirling-in-Rags").

Key sections in `aquiredJournalTasks`:
- Task acquisitions (which quests are active)
- Task resolutions (which quests are complete)
- Subtask states (individual objectives within quests)
- Check notifications (skill check results)

---

## 5. Party State (`.1st.ntwtf.json`)

```json
{
  "areaId": "Martinaise-ext",              // Current area
  "partyState": {
    "isKimInParty": true,                   // Kim following you
    "isKimLeftOutside": false,
    "isKimAbandoned": false,
    "isKimAwayUpToMorning": false,
    "isKimSleepingInHisRoom": false,
    "isCunoInParty": false,                 // Cuno following (late game)
    "isCunoLeftOutside": false,
    "isCunoAbandoned": true,
    "hasHangover": false,                   // Day 1 hangover
    "sleepLocation": 0,
    "waitLocation": 0
  }
}
```

---

## 6. Time & Day (`.2nd.ntwtf.json`)

Path: `sunshineClockTimeHolder.time`

```json
{
  "sunshineClockTimeHolder": {
    "time": {
      "dayCounter": 1,          // Game day (1-based)
      "realDayCounter": 1,      // Real day counter
      "dayMinutes": 777,        // Minutes since midnight (777 = 12:57)
      "seconds": 37             // Seconds within the minute
    }
  }
}
```

**Time conversion:** `dayMinutes / 60` = hours, `dayMinutes % 60` = minutes

---

## 7. Variables & Game State

### Reference: `output/game_assets/variables_all.json` (and categorized files)

Variables control dialogue flags, quest states, and game mechanics. They're stored at runtime in the `.ntwtf.lua` binary database and referenced by dialogues/scripts.

Categorized variable files:
- `variables_tasks.json` — 1,131 quest flags with human-readable descriptions
- `variables_xp.json` — 689 XP reward flags with point values
- `variables_reputation.json` — 21 political alignment + cop archetype counters
- `variables_character.json` — 111 character state flags
- `variables_stats.json` — 41 stats tracking variables
- `variables_inventory.json` — 266 inventory state flags
- `variables_locations.json` — 8,307 location-specific flags
- `variables_auto.json` — 56 auto-triggered flags
- `variables_globals.json` — 5 global state variables

---

## 8. World Interactions

Interaction orbs and clickable objects are tracked in two save file locations:
- `variousItemsHolder.Obsessions[]` — list of orb/interaction paths the player has seen
- `variousItemsHolder.DoorStates` — map of door names to open/closed booleans
- `containerSourceState.itemRegistry` — what items are in each container

---

## 9. Containers (`.2nd.ntwtf.json`)

Path: `containerSourceState.itemRegistry`

This section maps every lootable container to its contents. Keys are Unity scene object paths:

```json
{
  "containerSourceState": {
    "itemRegistry": {
      "Interactable whirl f2/Shelves11": [
        {
          "name": "pants_rcm",
          "probability": 1,
          "value": 0,
          "deviation": 0,
          "calculatedValue": 0,
          "bonusLoot": false
        }
      ]
    }
  }
}
```

Use `output/game_assets/items_inventory.json` to look up valid item IDs when modifying container contents.

---

## Common Editor Operations

### Give any item to the player
1. Look up item by `name` in `output/game_assets/items_inventory.json`
2. Add to `characterSheet.gainedItems[]` in `.2nd.ntwtf.json`
3. If wearable, optionally add to `equippedItems[]`

### Max all skills
1. For each skill in `characterSheet`, set `value` and `maximumValue` to desired level
2. Update `rankValue` to `value - calculatedAbility`

### Unlock all thoughts
1. Read all `name` values from `output/game_assets/items_thoughts.json`
2. Add to `characterSheet.gainedThoughts[]` and `fixedThoughts[]`
3. Update `thoughtCabinetState.thoughtListState[]` entries (state = `"FIXED"`, timeLeft = 0)
4. Update `thoughtCabinetState.thoughtCabinetViewState.slotStates[]`
5. Add skill bonuses to `SkillModifierCauseMap`

### Change time of day
1. Edit `sunshineClockTimeHolder.time.dayMinutes` (0-1439)
2. Edit `dayCounter` to change the day

### Set money
1. Edit `playerCharacter.Money` to desired amount

### Change party composition
1. Edit `.1st.ntwtf.json` → `partyState` booleans

---

## 10. Game Asset Reference Data

The `output/game_assets/` directory contains the complete static game database extracted from Unity asset bundles. This is the **definition** data — what things are, their names, descriptions, and bonuses. The save file stores which of these are active in a given playthrough.

### Mapping Save Variables to Definitions

| Save Variable | Definition Source | Purpose |
|---------------|-------------------|---------|
| Thought names | `output/game_assets/items_thoughts.json` | Look up thought bonuses, descriptions |
| `TASK.*` | `output/game_assets/variables_tasks.json` | Look up quest descriptions |
| Item names | `output/game_assets/items_inventory.json` | Item descriptions, equipment slots |
| `reputation.*` | `output/game_assets/variables_reputation.json` | Political alignment counter names |
| Skill keys | `output/game_assets/skill_key_map.json` | Save key ↔ type ↔ display name mapping |
| Actor IDs | `output/game_assets/actors_skills.json`, `actors_npcs_major.json` | Who's speaking |

### Reputation System

The game tracks **political alignment** via counters in `variables_reputation.json`:

| Variable | Description |
|----------|-------------|
| `reputation.communist` | Communist dialogue choices |
| `reputation.ultraliberal` | Ultraliberal dialogue choices |
| `reputation.moralist` | Moralist dialogue choices |
| `reputation.revacholian_nationhood` | Fascist/nationalist dialogue choices |
| `reputation.kim` | Kim Kitsuragi's opinion of you |
| `reputation.sorry_cop` | Sorry cop archetype counter |
| `reputation.superstar_cop` | Superstar cop archetype counter |
| `reputation.apocalypse_cop` | Apocalypse cop archetype counter |
| `reputation.boring_cop` | Boring cop archetype counter |
| `reputation.art_cop` | Art cop archetype counter |

### Thought Cabinet Details

`items_thoughts.json` provides full thought metadata:

```json
{
  "name": "hobocop",
  "thought_type": "other",
  "bonus_while_processing": "-1 Composure: Ungainly and rambling",
  "bonus_when_completed": "Reveals extra special collector's edition tare bottles on the map",
  "time_to_internalize": 180.0,
  "requirement": "Earn money through Can and Bottle Return"
}
```

This tells the editor what bonuses to apply in `SkillModifierCauseMap` when a thought is internalized.
