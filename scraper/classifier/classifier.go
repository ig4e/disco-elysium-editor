// Package classifier takes a flat map of parsed entities from the Disco Elysium
// Lua database and sorts them into strongly-typed categories for the save editor.
package classifier

import (
	"strings"
	"unicode"
)

// GameData holds all classified game entities.
type GameData struct {
	Items        map[string]map[string]interface{} `json:"items"`
	NPCs         map[string]map[string]interface{} `json:"npcs"`
	Thoughts     map[string]map[string]interface{} `json:"thoughts"`
	Skills       map[string]map[string]interface{} `json:"skills"`
	WorldObjects map[string]map[string]interface{} `json:"world_objects"`
	Unclassified map[string]map[string]interface{} `json:"unclassified"`

	// Flat key-value variables extracted from the top level
	TaskVariables      map[string]interface{} `json:"task_variables"`
	SubstanceVariables map[string]interface{} `json:"substance_variables"`
	GameVariables      map[string]interface{} `json:"game_variables"`
	AllVariables       map[string]interface{} `json:"all_variables"`
}

// NewGameData creates an empty GameData.
func NewGameData() *GameData {
	return &GameData{
		Items:              make(map[string]map[string]interface{}),
		NPCs:               make(map[string]map[string]interface{}),
		Thoughts:           make(map[string]map[string]interface{}),
		Skills:             make(map[string]map[string]interface{}),
		WorldObjects:       make(map[string]map[string]interface{}),
		Unclassified:       make(map[string]map[string]interface{}),
		TaskVariables:      make(map[string]interface{}),
		SubstanceVariables: make(map[string]interface{}),
		GameVariables:      make(map[string]interface{}),
		AllVariables:       make(map[string]interface{}),
	}
}

var skillNames = map[string]bool{
	"Logic": true, "Encyclopedia": true, "Rhetoric": true, "Drama": true,
	"Conceptualization": true, "Visual Calculus": true,
	"Volition": true, "Inland Empire": true, "Empathy": true, "Authority": true,
	"Suggestion": true, "Esprit de Corps": true,
	"Endurance": true, "Pain Threshold": true, "Physical Instrument": true,
	"Electrochemistry": true, "Shivers": true, "Half Light": true,
	"Hand/Eye Coordination": true, "Perception": true, "Reaction Speed": true,
	"Savoir Faire": true, "Interfacing": true, "Composure": true,
	// Ability attributes
	"Intellect": true, "Psyche": true, "Fysique": true, "Motorics": true,
	// Internal forms
	"Ancient Reptilian Brain": true, "Limbic System": true,
}

// Classify walks the parsed data tree and sorts entities into categories.
func Classify(data map[string]interface{}) *GameData {
	gd := NewGameData()

	// First pass: extract flat variables (TASK.*, SUBSTANCE.*, etc.)
	for key, val := range data {
		if _, isMap := val.(map[string]interface{}); isMap {
			continue // skip nested objects for this pass
		}
		// Flat key-value variable
		gd.AllVariables[key] = val
		if strings.HasPrefix(key, "TASK.") {
			gd.TaskVariables[key] = val
		} else if strings.HasPrefix(key, "SUBSTANCE.") {
			gd.SubstanceVariables[key] = val
		} else {
			gd.GameVariables[key] = val
		}
	}

	// Second pass: classify nested objects
	walkTree(data, "", gd)
	return gd
}

func walkTree(data map[string]interface{}, context string, gd *GameData) {
	for key, val := range data {
		obj, ok := val.(map[string]interface{})
		if !ok {
			continue // flat values already handled in first pass
		}

		classified := classifyObject(key, obj, gd)
		if !classified {
			// Recurse into sub-tables to find nested entities
			walkTree(obj, key, gd)
		}
	}
}

func classifyObject(key string, obj map[string]interface{}, gd *GameData) bool {
	name := stringVal(obj, "Name")
	if name == "" {
		name = key
	}

	// --- Skills / Abilities ---
	if skillNames[name] {
		if hasAny(obj, "IsNPC", "Is_Actor") {
			gd.Skills[key] = obj
			return true
		}
	}

	// --- NPCs ---
	if isTrue(obj["IsNPC"]) || isTrue(obj["Is_Actor"]) {
		gd.NPCs[key] = obj
		return true
	}

	// --- Thoughts (Thought Cabinet) ---
	if hasKey(obj, "thoughtType") || hasKey(obj, "isThought") ||
		hasKey(obj, "fixtureDescription") || hasKey(obj, "fixtureBonus") {
		gd.Thoughts[key] = obj
		return true
	}

	// --- Items ---
	if isItem(obj) {
		gd.Items[key] = obj
		return true
	}

	// --- World Objects (interactables with character_short_name but not items/NPCs) ---
	if hasKey(obj, "character_short_name") && hasKey(obj, "Articy_Id") {
		gd.WorldObjects[key] = obj
		return true
	}

	// --- Catch-all with Articy_Id ---
	if hasKey(obj, "Articy_Id") && name != "" {
		if !skillNames[name] {
			gd.Unclassified[key] = obj
			return true
		}
	}

	return false
}

func isItem(obj map[string]interface{}) bool {
	if isTrue(obj["IsItem"]) || isTrue(obj["Is_Item"]) {
		return true
	}
	if hasKey(obj, "itemType") || hasKey(obj, "itemGroup") {
		return true
	}
	if hasKey(obj, "itemValue") {
		return true
	}
	// Wearable / equipable items
	if hasKey(obj, "equipOrb") || hasKey(obj, "equipSlot") {
		return true
	}
	return false
}

// --- Helpers ---

func isTrue(v interface{}) bool {
	if b, ok := v.(bool); ok {
		return b
	}
	if s, ok := v.(string); ok {
		return strings.EqualFold(s, "true")
	}
	return false
}

func hasKey(m map[string]interface{}, key string) bool {
	_, ok := m[key]
	return ok
}

func hasAny(m map[string]interface{}, keys ...string) bool {
	for _, k := range keys {
		if _, ok := m[k]; ok {
			return true
		}
	}
	return false
}

func stringVal(m map[string]interface{}, key string) string {
	if v, ok := m[key]; ok {
		if s, ok := v.(string); ok {
			return strings.TrimFunc(s, func(r rune) bool {
				return unicode.IsControl(r) || r == 0
			})
		}
	}
	return ""
}
