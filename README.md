# Disco Elysium Save Editor

A comprehensive WinUI 3 save editor for Disco Elysium with full round-trip editing support.

## Features

### Character Management
- **Character Stats**: Full control over Intellect, Psyche, Physique, Motorics (base values and caps)
- **Skills**: Edit all 24 skill levels, XP, max skill command with one click
- **Resources**: XP amount, level, skill points, money (Réal), health, morale
- **Time**: In-game day, hour, and minute editing

### Inventory & Equipment
- **Container Inspector**: Browse loot tables for all 402 containers
- **Loot Editing**: Modify drop probabilities, item values, and potential deviations
- **Item Management**: Add/remove items from inventory with full catalog browsing
- **Equipment**: Manage equipped items (clothes, glasses, shoes, etc.) with automatic slot mapping
- **Item Search**: Filter owned items and catalog by name
- **Bulk Actions**: Add all items or remove non-quest items in one click

### Thought Cabinet
- **All 53 Thoughts**: View and edit state (Unknown, Known, Working, Internalized)
- **Bulk Operations**: Internalize all or reset all thoughts instantly
- **State Cycling**: Click to cycle through thought states

### Journal & Quests
- **Task Management**: View all journal tasks with complete/incomplete status
- **Search & Filter**: Find tasks by name
- **Task States**: Mark tasks as new, resolved, or in-progress
- **Bulk Actions**: Complete all or unresolve all tasks

### World & Variables
- **Interactive Objects**: Toggle Door states (Open/Locked) and Interaction Orbs
- **Area Progression**: detailed control over area states and exploration percentages
- **Lua Variable Browser**: View and edit all ~12,000 game variables from the binary database
- **Reputation System**: Quick-access editing for Communist, Ultraliberal, Moralist, Nationalist, and Kim reputation
- **Weather Control**: Set weather presets
- **Fog of War**: Toggle FoW cache visibility
- **Nested Tables**: Full support for editing nested variable paths (e.g., `reputation.communist`)

### Party & Game State
- **Party State**: Edit all 16 party state flags
- **Game Mode**: Switch between game modes
- **Location Flags**: Control location-specific states
- **HUD State**: Modify 12 HUD-related flags

### Failed White Checks
- **View Failed Checks**: See all failed passive skill checks with difficulty and skill values
- **Reset Checks**: Selectively reset failed checks to retry them in-game
- **Seen Checks**: Track which white checks have been seen

### Save Management
- **Undo/Redo**: Comprehensive undo/redo stack for safe editing
- **Auto-Discovery**: Automatically finds save files in default Disco Elysium save locations
- **Manual Open**: Browse to any `.ntwtf` save folder
- **Save As**: Export/copy saves to new locations
- **Automatic Backups**: Creates `.backup` copy before each save operation
- **Round-Trip Fidelity**: Preserves all unknown/future save data during edit cycles

## Installation

Download the latest executable from the [releases page](https://github.com/ig4e/disco-elysium-editor/releases) and run it.

**Requirements**: Windows 10/11 with **Windows App Runtime 1.8.x** installed.

## Technical Details

### Architecture
- **UI Framework**: WinUI 3 (unpackaged mode for simplified deployment)
- **MVVM**: CommunityToolkit.Mvvm with C# 13 partial properties
- **Save Format**: 
  - `.1st.ntwtf.json` — Party state, player character, FoW cache (JSON)
  - `.2nd.ntwtf.json` — Character sheet, inventory, thought cabinet, journal, HUD, weather (JSON)
  - `.ntwtf.lua` — ~12,000 Lua variables in binary TLV format
  - `.states.lua` — Area/container/conversation states (text Lua)
  - `.FOW.json` — Fog of War data (JSON)

### Build & Development

**Requirements:**
- .NET 8.0 SDK or later
- Windows 10 SDK (10.0.19041.0 or later)
- (Optional) Visual Studio 2022 with "Windows App Development" workload

**Building from source:**
```cmd
git clone https://github.com/Adversarian/disco-elysium-save-editor
cd disco-elysium-editor/DiscoSaveEditor
dotnet build
```

**Run:**
```cmd
dotnet run --project DiscoSaveEditor/DiscoSaveEditor.csproj
```

**Publish single-file executable:**
```cmd
dotnet publish DiscoSaveEditor/DiscoSaveEditor.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## Project Status

✅ **Complete Features:**
- [x] Port to C# / WinUI 3
- [x] Robust backup system
- [x] Binary .ntwtf.lua parser (TLV format matching Go reference implementation)
- [x] Full character sheet editor (stats, skills, XP, level)
- [x] Inventory management with equipment slot mapping
- [x] Thought Cabinet with state cycling
- [x] Journal/Task editing with bulk operations
- [x] World variable browser with nested table support
- [x] Reputation and weather quick-edit panels
- [x] Failed white checks viewer/reset
- [x] Party state and HUD editing
- [x] Save As / export functionality
- [x] Auto-discovery of save files
- [x] Container/loot editing (402 containers)
- [x] Door states, Area states, and Orb visibility editing
- [x] Undo/redo system

🚧 **Future Enhancements:**
- [ ] Multiple backup rotation

## Contributing

Feature requests and bug reports are welcome via [issues](https://github.com/ig4e/disco-elysium-editor/issues).

Pull requests are accepted — the codebase follows standard MVVM patterns with ViewModels in `ViewModels/`, Views in `Views/`, Models in `Models/`, and Services in `Services/`.

## License

See [LICENSE](LICENSE) file.

