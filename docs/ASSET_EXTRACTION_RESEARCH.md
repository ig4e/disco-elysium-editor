# Disco Elysium — Asset Bundle Extraction Research

> Research report covering tools, serialization format, data model, localization,
> and community landscape for extracting dialogue data from Disco Elysium's
> Unity asset bundles.

---

## Executive Summary

Disco Elysium uses the **PixelCrushers Dialogue System for Unity v2.2.65** to
store its entire dialogue tree. The dialogue database is a Unity
`ScriptableObject` serialized into standard Unity binary format inside
asset bundles. It can be extracted to JSON using **UnityPy** (Python).

**Critical finding:** This copy of the game is an **IL2CPP build** (not Mono).
There is no `disco_Data/Managed/` directory — instead the game has
`disco_Data/il2cpp_data/` with `global-metadata.dat`. This affects how
MonoBehaviour type trees are reconstructed (see §1 for details).

---

## Table of Contents

1. [Tools for Extracting Serialized MonoBehaviour Data](#1-tools)
2. [How PixelCrushers Serializes Its DialogueDatabase](#2-serialization)
3. [Data Fields in a PixelCrushers Dialogue Database](#3-data-fields)
4. [Can UnityPy Extract Dialogue Data to JSON?](#4-unitypy-to-json)
5. [Are Localization Bundles Readable Text?](#5-localization)
6. [Community Efforts & Landscape](#6-community)
7. [Recommended Approach](#7-approach)

---

## 1. Tools for Extracting Serialized MonoBehaviour Data {#1-tools}

### Python: UnityPy (recommended)

- **Repo:** [K0lb3/UnityPy](https://github.com/K0lb3/UnityPy) — ~1.2k stars
- **Install:** `pip install UnityPy`
- **What it does:** Pure-Python library that reads UnityFS bundles, `.assets`
  files, and serialized objects. Handles all standard Unity types natively
  (Texture2D, AudioClip, TextAsset, Mesh, etc.) and can extract MonoBehaviour
  objects via type tree reconstruction.
- **MonoBehaviour support:** For custom C# types (like `DialogueDatabase`),
  UnityPy needs the type tree to know how to deserialize the binary blob.
  Two mechanisms:
  1. **Embedded type trees** — Some bundles include the type tree in the
     header. If present, `obj.read()` and `obj.read_typetree()` just work.
  2. **TypeTreeGenerator** — When type trees are NOT embedded, you must
     supply them. UnityPy ships a `TypeTreeGenerator` module that
     reconstructs type trees from managed DLLs.

#### IL2CPP Complication

This game is built with IL2CPP, meaning there are **no managed `.dll` files**
in `disco_Data/Managed/`. The type information is instead baked into
`disco_Data/il2cpp_data/Metadata/global-metadata.dat`.

**Workarounds:**

| Approach | Description |
|----------|-------------|
| **Il2CppDumper** | Use [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) to recover C# type definitions from `GameAssembly.dll` + `global-metadata.dat`. This produces dummy `.dll` assemblies that UnityPy's TypeTreeGenerator can consume. |
| **Embedded type trees** | Try loading the bundle first — IL2CPP Unity builds sometimes embed type trees in asset bundles. If `obj.read_typetree()` works without extra setup, no DLL recovery is needed. |
| **Il2CppInterop/cpp2il** | [cpp2il](https://github.com/SamboyCoding/Cpp2IL) is a more modern alternative to Il2CppDumper for recovering assemblies. |
| **Manual type tree** | Define the type tree by hand based on the PixelCrushers API docs (documented below). This is fragile but doable for a single class. |

**Recommended first step:** Load the dialogue bundle and check whether type
trees are embedded. If `obj.read_typetree()` returns data, skip DLL recovery
entirely.

### .NET: AssetStudio

- **Repo:** [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio)
  (archived, community forks active)
- **What it does:** .NET GUI application for browsing Unity assets. Can export
  MonoBehaviour objects as JSON dumps. Good for quick inspection but harder to
  script for batch extraction.
- **IL2CPP support:** AssetStudio generally handles IL2CPP games because it
  relies on embedded type trees or its own type database.

### Go: No Viable Library

There is **no production-quality Go library** for reading Unity asset bundles
or deserializing MonoBehaviour data. The plan should be:

1. Use Python/UnityPy for the initial extraction pass
2. Export to JSON
3. Consume the JSON from Go for the save editor

This is the same pattern the existing scraper uses (Go consuming structured
JSON), so it fits naturally.

### Key DLLs Present (from ScriptingAssemblies.json)

Even though managed DLLs aren't on disk (IL2CPP), the assembly manifest tells
us the relevant assemblies:

| Assembly | Purpose |
|----------|---------|
| `DialogueSystem.dll` | PixelCrushers Dialogue System (the dialogue database classes) |
| `PixelCrushers.dll` | PixelCrushers common framework |
| `Assembly-CSharp.dll` | Game-specific C# code |
| `l2Localization.dll` | I2 Localization (alternative localization system) |
| `FullSerializer.dll` | JSON serializer used by PixelCrushers internally |
| `FeldMigration.dll` | Custom game migration logic |
| `DiscoGlobals.dll` | Game global state |
| `ConditionChecker.dll` | Condition evaluation system |

---

## 2. How PixelCrushers Serializes Its DialogueDatabase {#2-serialization}

### Format: Standard Unity Serialization (NOT JSON, NOT custom binary)

`DialogueDatabase` is a C# class that extends `ScriptableObject`. Unity
serializes it using its **standard binary serialization** — the same format
used for every Unity asset. This means:

- It is **not** JSON (though PixelCrushers can export/import JSON at edit-time)
- It is **not** a custom binary format
- It follows Unity's field-by-field serialization rules
- All `[SerializeField]` and public fields are serialized
- Unity handles polymorphism, references, arrays, lists, and nested objects

When packed into an asset bundle, the `DialogueDatabase` becomes a
**MonoBehaviour** (technically a ScriptableObject, which Unity treats as a
MonoBehaviour subclass for serialization purposes). To read it, you need the
type tree.

### What's Inside a DialogueDatabase

The top-level `DialogueDatabase` object contains these serialized collections:

```
DialogueDatabase : ScriptableObject
├── version        : string
├── author         : string
├── description    : string
├── baseID         : int
├── globalUserScript : string           // Global Lua code run at DB load
├── emphasisSettings : EmphasisSetting[] // Text formatting definitions
├── actors         : List<Actor>
├── items          : List<Item>         // Also used for quests
├── locations      : List<Location>
├── variables      : List<Variable>     // Lua variables with defaults
└── conversations  : List<Conversation>
    └── dialogueEntries : List<DialogueEntry>
        └── outgoingLinks : List<Link>
```

Every entity (Actor, Item, Location, Variable, Conversation, DialogueEntry)
carries a `List<Field>` — a flat list of key-value-type tuples that stores
**all** its attribute data.

---

## 3. Data Fields in a PixelCrushers Dialogue Database {#3-data-fields}

### The Field System

The fundamental data element in PixelCrushers is the **`Field`** class:

```
Field
├── title     : string      // Field name (e.g., "Dialogue Text", "Name")
├── value     : string      // Field value (always stored as string)
├── type      : FieldType   // Enum: Text, Number, Boolean, Actor, Item,
│                           //   Location, Files, Localization, CustomFieldType_*
└── typeString : string     // String representation of type
```

All entity data is stored as lists of `Field` objects. There are no dedicated
C# properties at the serialization level — everything goes through the `fields`
list. The C# properties like `DialogueText`, `ActorID`, etc. are just
convenience accessors that call `Field.Lookup(fields, "Dialogue Text")`.

### Actor Fields

| Field Title | Type | Description |
|------------|------|-------------|
| `Name` | Text | Character display name |
| `Pictures` | Files | Portrait texture references |
| `Description` | Text | Character description |
| `IsPlayer` | Boolean | `true` for the player character |
| `IsNPC` | Boolean | `true` for non-player characters |
| `IsFemale` | Boolean | Gender flag |
| `Articy_Id` | Text | Articy:Draft cross-reference ID |

Additional runtime properties on the Actor class:
- `portrait` (Texture2D) — portrait image
- `spritePortrait` (Sprite) — sprite-based portrait
- `alternatePortraits` (List\<Texture2D\>) — alternate portrait images
- `textureName` (string) — portrait texture name

### Conversation Fields

| Field Title | Type | Description |
|------------|------|-------------|
| `Title` | Text | Conversation identifier (e.g., "YOURSKILLS_SHI-SHIVERS-INTRO") |
| `Actor` | Actor | Primary participant (stored as actor ID) |
| `Conversant` | Actor | Other participant (stored as actor ID) |

Additional serialized fields on the Conversation class:
- `dialogueEntries` (List\<DialogueEntry\>) — all nodes in this conversation
- `overrideSettings` (ConversationOverrideDisplaySettings) — UI overrides
- `nodeColor` — editor canvas color
- `canvasScrollPosition` (Vector2) — editor layout data
- `canvasZoom` (float) — editor layout data
- `entryGroups` (List\<EntryGroup\>) — node groupings

### DialogueEntry Fields

| Field Title | Type | Description |
|------------|------|-------------|
| `Title` | Text | Node title / label |
| `Dialogue Text` | Text | The actual spoken line |
| `Menu Text` | Text | Short version shown in response menus |
| `Sequence` | Text | Sequencer commands (camera, audio, animation) |
| `Response Menu Sequence` | Text | Sequence played during response menu |
| `Actor` | Actor | Who speaks this line (as actor ID) |
| `Conversant` | Actor | Who is being spoken to (as actor ID) |
| `Video File` | Text | Associated video |
| `Audio Files` | Files | Voice-over audio references |
| `Animation Files` | Files | Animation clip references |
| `Lipsync Files` | Files | Lip-sync data references |
| `Articy_Id` | Text | Cross-reference to Articy:Draft |

Additional serialized fields on the DialogueEntry class:
- `id` (int) — unique ID within the conversation
- `conversationID` (int) — parent conversation ID
- `isRoot` (bool) — is this the START node?
- `isGroup` (bool) — is this a group/folder node?
- `conditionsString` (string) — **Lua condition** (e.g., skill checks)
- `userScript` (string) — **Lua code** executed when this node is reached
- `falseConditionAction` (string) — what to do when condition fails
- `conditionPriority` (int) — priority for condition evaluation
- `outgoingLinks` (List\<Link\>) — connections to next nodes
- `canvasRect` (Rect) — editor position
- `nodeColor` — editor color
- `delaySimStatus` (bool) — delay simulation status update

### Link (Edge Between Dialogue Entries)

```
Link
├── originConversationID  : int
├── originDialogueID      : int
├── destinationConversationID : int
├── destinationDialogueID : int
├── isConnector           : bool  // true = cross-conversation link
└── priority              : int   // ConditionPriority enum
```

### Item / Quest Fields

Items and quests share the same class (`Item`). Items have standard inventory
fields; quests use the same structure with quest-specific fields.

| Field Title | Type | Description |
|------------|------|-------------|
| `Name` | Text | Item/quest name |
| `Description` | Text | Item/quest description |
| `Is Item` | Boolean | `true` for inventory items |
| `itemGroup` | Text | Category (e.g., "PSY", "THOUGHT") |
| `itemType` | Text | Type (e.g., "EQUIPMENT", "CONSUMABLE") |
| `itemValue` | Number | Monetary value |
| `Articy_Id` | Text | Cross-reference ID |

### Variable Fields

| Field | Type | Description |
|-------|------|-------------|
| `Name` | Text | Variable name (e.g., "TASK.FIND_BADGE_done") |
| `Initial Value` | varies | Default value |
| `Description` | Text | What this variable tracks |

### Location Fields

| Field Title | Type | Description |
|------------|------|-------------|
| `Name` | Text | Location name |
| `Description` | Text | Location description |
| `Articy_Id` | Text | Cross-reference ID |

### Localization via Field Naming Convention

PixelCrushers uses a naming convention for localized field variants:

- Base field: `"Dialogue Text"` (default language)
- French: `"Dialogue Text fr"`
- German: `"Dialogue Text de"`
- etc.

Localized values are resolved via `Field.LookupLocalizedValue()` and
`Field.LocalizedTitle()`. The `currentLocalized*` properties on
DialogueEntry return the appropriate localized version based on the
active language.

---

## 4. Can UnityPy Extract Dialogue Data to JSON? {#4-unitypy-to-json}

**Yes**, with caveats related to the IL2CPP build.

### Basic Extraction Pattern

```python
import UnityPy
import json

env = UnityPy.load("path/to/dialoguebundle_assets_all_*.bundle")

for obj in env.objects:
    if obj.type.name == "MonoBehaviour":
        # Attempt to read the type tree
        try:
            data = obj.read_typetree()
            tree = obj.read_typetree()  # returns dict
            with open("dialogue_database.json", "w") as f:
                json.dump(tree, f, indent=2, default=str)
        except Exception as e:
            print(f"Type tree not embedded: {e}")
            # Need Il2CppDumper route
```

### If Type Trees Are NOT Embedded

1. Run **Il2CppDumper** on `GameAssembly.dll` + `global-metadata.dat`:
   ```
   Il2CppDumper.exe GameAssembly.dll global-metadata.dat output_dir
   ```
   This produces dummy managed assemblies in `output_dir/DummyDll/`.

2. Feed the dummy DLLs to UnityPy's TypeTreeGenerator:
   ```python
   from UnityPy.helpers import TypeTreeGenerator
   
   generator = TypeTreeGenerator()
   generator.load_local("path/to/DummyDll/DialogueSystem.dll")
   generator.load_local("path/to/DummyDll/PixelCrushers.dll")
   generator.load_local("path/to/DummyDll/Assembly-CSharp.dll")
   # ... load all dependencies
   
   env = UnityPy.load("path/to/dialoguebundle.bundle")
   for obj in env.objects:
       if obj.type.name == "MonoBehaviour":
           data = obj.read_typetree(generator)
           # Now data is a dict you can json.dump()
   ```

### The Dialogue Bundle

- **File:** `disco_Data/StreamingAssets/aa/StandaloneWindows64/dialoguebundle_assets_all_3472cb598f88f38eef12cdb3aa5fdc80.bundle`
- **Size:** 10.9 MB
- This almost certainly contains the entire `DialogueDatabase` ScriptableObject.
  At 10.9 MB, it's large enough to hold all conversations, entries, actors,
  items, variables, and links for the entire game.

### What the JSON Output Will Look Like

Based on the data model, the extracted JSON will have a structure like:

```json
{
  "m_Name": "DialogueDatabase",
  "version": "...",
  "author": "...",
  "globalUserScript": "...",
  "actors": [
    {
      "id": 1,
      "fields": [
        { "title": "Name", "value": "Harry Du Bois", "type": 0 },
        { "title": "IsPlayer", "value": "True", "type": 2 },
        { "title": "Description", "value": "...", "type": 0 }
      ]
    }
  ],
  "conversations": [
    {
      "id": 1,
      "fields": [
        { "title": "Title", "value": "YOURSKILLS_SHI-SHIVERS-INTRO", "type": 0 },
        { "title": "Actor", "value": "1", "type": 3 }
      ],
      "dialogueEntries": [
        {
          "id": 0,
          "conversationID": 1,
          "isRoot": true,
          "fields": [
            { "title": "Dialogue Text", "value": "The city speaks to you...", "type": 0 }
          ],
          "conditionsString": "Variable[\"PSY\"] >= 3",
          "userScript": "Variable[\"SHIVERS_INTRO_done\"] = true",
          "outgoingLinks": [
            {
              "originConversationID": 1,
              "originDialogueID": 0,
              "destinationConversationID": 1,
              "destinationDialogueID": 1,
              "isConnector": false,
              "priority": 2
            }
          ]
        }
      ]
    }
  ],
  "variables": [
    {
      "id": 1,
      "fields": [
        { "title": "Name", "value": "TASK.FIND_BADGE_done", "type": 0 },
        { "title": "Initial Value", "value": "false", "type": 2 }
      ]
    }
  ],
  "items": [ ... ],
  "locations": [ ... ]
}
```

---

## 5. Are Localization Bundles Readable Text? {#5-localization}

### Two Localization Systems

Disco Elysium has **two** localization systems:

#### A. Lockits Bundles (standalone asset bundles)

Location: `disco_Data/StreamingAssets/AssetBundles/Windows/lockits/`

| Language Bundle | Has Manifest |
|----------------|--------------|
| `lockit_arabic` | ✓ |
| `lockit_chinese` | ✓ |
| `lockit_french` | ✓ |
| `lockit_japanese` | ✓ |
| `lockit_korean` | ✓ |
| `lockit_polish` | ✓ |
| `lockit_portuguese` | ✓ |
| `lockit_russian` | ✓ |
| `lockit_spanish` | ✓ |
| `lockit_traditionalchinese` | ✓ |
| `lockit_turkish` | ✓ |

These are **standard Unity asset bundles** (NOT in the Addressables system).
They likely contain either:
- **TextAsset** objects — plain text files (CSV, JSON, or XML) embedded as
  Unity TextAssets. If so, UnityPy can extract them trivially.
- **MonoBehaviour / ScriptableObject** — PixelCrushers localization data
  objects. Would need the same type-tree approach.

The `I2 Localization` system (assembly `l2Localization.dll`) is also present
and may be used for UI strings separate from dialogue.

**These bundles are absolutely readable** via UnityPy — the question is just
whether the contained assets are TextAssets (trivial) or MonoBehaviours
(need type trees). A quick probe will determine this.

#### B. Inline Localization (Field naming convention)

PixelCrushers also supports inline localized fields within the dialogue
database itself. Instead of having separate localization files, translated
text can be stored as additional Field entries alongside the originals:

- `"Dialogue Text"` → English (default)
- `"Dialogue Text fr"` → French
- `"Dialogue Text de"` → German
- `"Dialogue Text es"` → Spanish

The `currentLocalizedDialogueText` property on `DialogueEntry` resolves the
correct field based on the active language. When extracting the dialogue
database to JSON, **all localized variants will be included** in the `fields`
array naturally.

#### Note: English is Unlisted

English is conspicuously absent from the `lockits/` directory. This strongly
suggests English is the **base language** stored directly in the dialogue
database, and the lockits bundles contain override translations for other
languages.

---

## 6. Community Efforts & Landscape {#6-community}

### GitHub Repositories

| Repository | Stars | Language | Relevance |
|-----------|-------|----------|-----------|
| [sailro/DiscoElysium-Trainer](https://github.com/sailro/DiscoElysium-Trainer) | 13 | C# | **Most relevant.** Runtime Mono-injection trainer using SharpMonoInjector. Manipulates `PlayerCharacter`, `CharsheetView`, abilities/skills at runtime. Uses namespaces `Sunshine.Metric`, `Voidforge`, `LocalizationCustomSystem`, `Sunshine.Views`. **Archived.** No data extraction. |
| [tparker48/Disco-Elysium-Mod](https://github.com/tparker48/Disco-Elysium-Mod) | 1 | C# | Harmony-based mod. Unknown scope. |
| [michael-y03/Disco-Elysium-Ultrawide-Fix](https://github.com/michael-y03/Disco-Elysium-Ultrawide-Fix) | 1 | — | Ultrawide resolution fix only. |

### NexusMods

~36 mods total. Predominantly:
- Portrait/visual replacements
- Translation patches
- Minor QoL tweaks

**No dedicated data extraction tools or dialogue database dumps exist.**

### Key Takeaway

There is **no existing public tool or data dump** for extracting the Disco
Elysium dialogue database from asset bundles. This would be novel work.

The sailro/DiscoElysium-Trainer repo is useful for confirming the game's
internal namespace structure:
- `Voidforge` — the game's custom framework namespace
- `Sunshine.Metric` — character stats/metrics
- `Sunshine.Views` — UI views
- `LocalizationCustomSystem` — custom localization layer

---

## 7. Recommended Approach {#7-approach}

### Phase 1: Probe (30 minutes)

1. Install UnityPy: `pip install UnityPy`
2. Load the dialogue bundle:
   ```python
   env = UnityPy.load("disco_Data/StreamingAssets/aa/StandaloneWindows64/"
                       "dialoguebundle_assets_all_3472cb598f88f38eef12cdb3aa5fdc80.bundle")
   for obj in env.objects:
       print(obj.type.name, obj.byte_size)
   ```
3. Try `obj.read_typetree()` — if type trees are embedded, you're already done.
4. Similarly probe one lockit bundle to see if it contains TextAssets or
   MonoBehaviours.

### Phase 2: IL2CPP Recovery (if needed, ~1 hour)

If type trees are not embedded:
1. Run [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) on:
   - `GameAssembly.dll` (in the game's root directory)
   - `disco_Data/il2cpp_data/Metadata/global-metadata.dat`
2. Produces dummy DLLs in `DummyDll/` directory
3. Feed to UnityPy's TypeTreeGenerator
4. Re-attempt extraction

### Phase 3: Extract & Transform (1–2 hours)

1. Extract full dialogue database to raw JSON
2. Build a transformer script that reshapes the raw PixelCrushers format
   (nested `fields` arrays with string values) into a clean, strongly-typed
   JSON structure
3. Cross-reference with the existing save file extractor's output
   (`output/npcs.json`, `output/items.json`, etc.) using `Articy_Id` and
   entity names

### Phase 4: Integrate with Go Save Editor

The extracted JSON becomes a static reference catalog:
- **All dialogue lines** with their conditions and scripts
- **All variable definitions** with initial values
- **All actors** with their relationships
- **Conversation graph** (who talks to whom, in what order, with what checks)
- **Quest/task definitions** matching the `TASK.*` variables already extracted

This catalog tells the save editor what values are valid, what variables
exist, and what game state means in context.

### Per-NPC Bundles

Beyond the main dialogue bundle, the Addressables catalog reveals per-NPC
bundles (e.g., `apt_authority_assets_all_*.bundle`,
`apt-orb_kim-kitsuragi_assets_all_*.bundle`). These may contain:
- Per-character dialogue trees (split from the main database)
- Voice-over audio references
- Character-specific assets

The 20 MB `catalog.json` at
`disco_Data/StreamingAssets/aa/catalog.json` maps internal IDs to bundles
and contains ~51,379 entries including voice-over WAV paths. This catalog
is essential for understanding which bundle contains what.

---

## File Reference

| File | Location | Size | Purpose |
|------|----------|------|---------|
| Dialogue bundle | `aa/StandaloneWindows64/dialoguebundle_assets_all_*.bundle` | 10.9 MB | Main dialogue database |
| Addressables catalog | `aa/catalog.json` | 20 MB | Asset-to-bundle mapping (51k entries) |
| IL2CPP metadata | `il2cpp_data/Metadata/global-metadata.dat` | — | Type metadata for IL2CPP recovery |
| Lockit bundles | `AssetBundles/Windows/lockits/lockit_*` | — | 11 language translation overlays |
| Shared bundles | `AssetBundles/Windows/shared/` | — | Shared game assets |
| All Addressable bundles | `aa/StandaloneWindows64/*.bundle` | ~3.8 GB | 1,828 asset bundles total |

---

## Appendix: Relevant Assemblies in the Game

From `ScriptingAssemblies.json` — the assemblies that matter for extraction:

| Assembly | Purpose |
|----------|---------|
| `DialogueSystem.dll` | PixelCrushers Dialogue System core |
| `PixelCrushers.dll` | PixelCrushers common framework |
| `Assembly-CSharp.dll` | Game logic (Voidforge, Sunshine namespaces) |
| `FullSerializer.dll` | JSON serialization (used by PixelCrushers at edit-time) |
| `l2Localization.dll` | I2 Localization system |
| `FeldMigration.dll` | Database migration logic |
| `DiscoGlobals.dll` | Game global state definitions |
| `ConditionChecker.dll` | Lua condition evaluation |
| `InControl.dll` | Input handling |
| `MasterAudio.dll` | Audio system |
| `Newtonsoft.Json.dll` | JSON library |
