// Disco Elysium Game Database Extractor
//
// This tool parses the binary-serialized Lua database embedded in save files
// (.ntwtf.lua) and extracts every game entity into clean, categorized JSON
// files ready for use in a save editor.
//
// Usage:
//
//	go run . <path-to-ntwtf.lua-file>
//	go run . <path-to-ntwtf-save-folder>
//
// It will auto-detect whether you passed a .lua file or a .ntwtf save folder.
// Output goes to ../output/ relative to the scraper directory.
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"

	"disco-scraper/classifier"
	"disco-scraper/cleaner"
	"disco-scraper/parser"
)

func main() {
	if len(os.Args) < 2 {
		fmt.Println("Disco Elysium Game Database Extractor")
		fmt.Println("======================================")
		fmt.Println()
		fmt.Println("Usage:")
		fmt.Println("  go run . <path-to-file.ntwtf.lua>")
		fmt.Println("  go run . <path-to-save-folder.ntwtf>")
		fmt.Println()
		fmt.Println("The tool will parse the binary Lua database and extract all game")
		fmt.Println("entities (items, NPCs, thoughts, tasks, variables, dialogues, etc.)")
		fmt.Println("into categorized JSON files in the output/ directory.")
		os.Exit(1)
	}

	inputPath := os.Args[1]
	luaFile, err := findLuaFile(inputPath)
	if err != nil {
		fmt.Printf("Error: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("Reading: %s\n", luaFile)
	data, err := os.ReadFile(luaFile)
	if err != nil {
		fmt.Printf("Error reading file: %v\n", err)
		os.Exit(1)
	}
	fmt.Printf("File size: %.2f MB\n", float64(len(data))/1024/1024)

	// ---- Parse the binary Lua database ----
	fmt.Println("\n[1/4] Parsing binary Lua TLV stream...")
	start := time.Now()
	p := parser.New(data)
	allData, err := p.ReadAll()
	if err != nil {
		fmt.Printf("Parse error: %v\n", err)
		os.Exit(1)
	}
	fmt.Printf("  Parsed %d top-level entries in %v\n", len(allData), time.Since(start))

	// ---- Classify entities ----
	fmt.Println("\n[2/4] Classifying entities...")
	start = time.Now()
	gd := classifier.Classify(allData)
	fmt.Printf("  Classification complete in %v\n", time.Since(start))

	// ---- Clean text ----
	fmt.Println("\n[3/4] Cleaning text fields...")
	gd.Items = cleaner.CleanMap(gd.Items)
	gd.NPCs = cleaner.CleanMap(gd.NPCs)
	gd.Thoughts = cleaner.CleanMap(gd.Thoughts)
	gd.WorldObjects = cleaner.CleanMap(gd.WorldObjects)
	gd.Skills = cleaner.CleanMap(gd.Skills)
	gd.Unclassified = cleaner.CleanMap(gd.Unclassified)

	// ---- Write output ----
	fmt.Println("\n[4/4] Writing output files...")

	// Determine output dir: ../output/ relative to the scraper directory
	exeDir, _ := os.Getwd()
	outDir := filepath.Join(exeDir, "..", "output")
	os.MkdirAll(outDir, 0755)

	writeJSON(outDir, "items.json", gd.Items)
	writeJSON(outDir, "npcs.json", gd.NPCs)
	writeJSON(outDir, "thoughts.json", gd.Thoughts)
	writeJSON(outDir, "skills.json", gd.Skills)
	writeJSON(outDir, "world_objects.json", gd.WorldObjects)
	writeJSON(outDir, "task_variables.json", gd.TaskVariables)
	writeJSON(outDir, "substance_variables.json", gd.SubstanceVariables)
	writeJSON(outDir, "game_variables.json", gd.GameVariables)
	writeJSON(outDir, "all_variables.json", gd.AllVariables)
	writeJSON(outDir, "unclassified.json", gd.Unclassified)

	// Also write the full raw parse for debugging
	writeJSON(outDir, "_raw_full_database.json", allData)

	// Write a manifest / summary
	summary := map[string]interface{}{
		"source_file":             luaFile,
		"extracted_at":            time.Now().Format(time.RFC3339),
		"total_top_level":         len(allData),
		"items_count":             len(gd.Items),
		"npcs_count":              len(gd.NPCs),
		"thoughts_count":          len(gd.Thoughts),
		"skills_count":            len(gd.Skills),
		"world_objects_count":     len(gd.WorldObjects),
		"task_variables_count":    len(gd.TaskVariables),
		"substance_vars_count":   len(gd.SubstanceVariables),
		"game_variables_count":   len(gd.GameVariables),
		"all_variables_count":    len(gd.AllVariables),
		"unclassified_count":     len(gd.Unclassified),
	}
	writeJSON(outDir, "_manifest.json", summary)

	// Print summary
	fmt.Println()
	fmt.Println("=== Extraction Complete ===")
	fmt.Printf("  Items:              %d\n", len(gd.Items))
	fmt.Printf("  NPCs:               %d\n", len(gd.NPCs))
	fmt.Printf("  Thoughts:           %d\n", len(gd.Thoughts))
	fmt.Printf("  Skills:             %d\n", len(gd.Skills))
	fmt.Printf("  World Objects:      %d\n", len(gd.WorldObjects))
	fmt.Printf("  Task Variables:     %d\n", len(gd.TaskVariables))
	fmt.Printf("  Substance Vars:     %d\n", len(gd.SubstanceVariables))
	fmt.Printf("  Game Variables:     %d\n", len(gd.GameVariables))
	fmt.Printf("  All Variables:      %d\n", len(gd.AllVariables))
	fmt.Printf("  Unclassified:       %d\n", len(gd.Unclassified))
	fmt.Printf("\nOutput directory: %s\n", outDir)
}

func findLuaFile(input string) (string, error) {
	info, err := os.Stat(input)
	if err != nil {
		return "", fmt.Errorf("cannot access %s: %w", input, err)
	}

	if !info.IsDir() {
		// Direct file path
		if strings.HasSuffix(input, ".lua") {
			return input, nil
		}
		return "", fmt.Errorf("expected a .lua file, got: %s", input)
	}

	// It's a directory — scan for .ntwtf.lua files
	entries, err := os.ReadDir(input)
	if err != nil {
		return "", err
	}
	for _, e := range entries {
		if strings.HasSuffix(e.Name(), ".ntwtf.lua") {
			return filepath.Join(input, e.Name()), nil
		}
	}
	return "", fmt.Errorf("no .ntwtf.lua file found in directory: %s", input)
}

func writeJSON(dir, filename string, data interface{}) {
	path := filepath.Join(dir, filename)
	f, err := os.Create(path)
	if err != nil {
		fmt.Printf("  ERROR creating %s: %v\n", filename, err)
		return
	}
	defer f.Close()

	enc := json.NewEncoder(f)
	enc.SetIndent("", "  ")
	enc.SetEscapeHTML(false)
	if err := enc.Encode(data); err != nil {
		fmt.Printf("  ERROR writing %s: %v\n", filename, err)
		return
	}

	// Get file size
	info, _ := f.Stat()
	size := int64(0)
	if info != nil {
		size = info.Size()
	}
	fmt.Printf("  %-30s written (%s)\n", filename, humanSize(size))
}

func humanSize(b int64) string {
	if b == 0 {
		return "0 B"
	}
	const unit = 1024
	if b < unit {
		return fmt.Sprintf("%d B", b)
	}
	div, exp := int64(unit), 0
	for n := b / unit; n >= unit; n /= unit {
		div *= unit
		exp++
	}
	return fmt.Sprintf("%.1f %cB", float64(b)/float64(div), "KMG"[exp])
}
