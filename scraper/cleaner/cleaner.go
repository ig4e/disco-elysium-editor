// Package cleaner provides utilities to sanitize extracted string data from the
// Disco Elysium database. The game embeds control characters (0x01-0x04) in
// text fields that serve as internal formatting markers.
package cleaner

import (
	"strings"
	"unicode"
)

// CleanString removes leading control characters and trims whitespace.
func CleanString(s string) string {
	return strings.TrimFunc(s, func(r rune) bool {
		return unicode.IsControl(r) || r == 0
	})
}

// CleanObject recursively cleans all string values in a map.
func CleanObject(obj map[string]interface{}) map[string]interface{} {
	cleaned := make(map[string]interface{}, len(obj))
	for k, v := range obj {
		switch val := v.(type) {
		case string:
			cleaned[k] = CleanString(val)
		case map[string]interface{}:
			cleaned[k] = CleanObject(val)
		default:
			cleaned[k] = v
		}
	}
	return cleaned
}

// CleanMap cleans all objects in a keyed map.
func CleanMap(m map[string]map[string]interface{}) map[string]map[string]interface{} {
	result := make(map[string]map[string]interface{}, len(m))
	for k, v := range m {
		result[k] = CleanObject(v)
	}
	return result
}
