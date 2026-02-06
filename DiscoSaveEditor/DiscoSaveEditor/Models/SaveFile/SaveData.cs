using System.Text.Json.Nodes;

namespace DiscoSaveEditor.Models.SaveFile;

/// <summary>
/// Aggregate container holding all data from a save folder.
/// </summary>
public class SaveData
{
    public string FolderPath { get; set; } = "";
    public string BaseName { get; set; } = "";
    public FirstFile First { get; set; } = new();
    public SecondFile Second { get; set; } = new();

    /// <summary>Raw JsonNode for round-trip editing (preserves property order)</summary>
    public JsonNode? FirstRaw { get; set; }
    public JsonNode? SecondRaw { get; set; }

    /// <summary>Binary Lua database (~12,000 runtime variables)</summary>
    public Dictionary<string, object> LuaDatabase { get; set; } = new();

    /// <summary>States.lua parsed data</summary>
    public StatesData States { get; set; } = new();
}

public class StatesData
{
    public Dictionary<string, int> AreaStates { get; set; } = new();
    public Dictionary<string, int> ShownOrbs { get; set; } = new();
}
