using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

public class InventoryState
{
    [JsonPropertyName("itemListState")] public List<ItemState> ItemListState { get; set; } = new();
    [JsonPropertyName("inventoryViewState")] public InventoryViewState InventoryViewState { get; set; } = new();
    [JsonPropertyName("wearingBodysuit")] public bool WearingBodysuit { get; set; }
}

public class ItemState
{
    [JsonPropertyName("itemName")] public string ItemName { get; set; } = "";
    [JsonPropertyName("isFresh")] public bool IsFresh { get; set; }
    [JsonPropertyName("substanceUses")] public int SubstanceUses { get; set; }
    [JsonPropertyName("substanceTimeLeft")] public int SubstanceTimeLeft { get; set; }
    [JsonPropertyName("StackItems")] public JsonElement? StackItems { get; set; }
}

public class InventoryViewState
{
    [JsonPropertyName("equipment")] public Dictionary<string, string> Equipment { get; set; } = new();
    [JsonPropertyName("inventory")] public Dictionary<string, List<InventorySlot>> Inventory { get; set; } = new();
    [JsonPropertyName("bullets")] public int Bullets { get; set; }
    [JsonPropertyName("keys")] public List<string> Keys { get; set; } = new();
    [JsonPropertyName("lastSelectedItem")] public string LastSelectedItem { get; set; } = "";
}

public class InventorySlot
{
    [JsonPropertyName("Key")] public int Key { get; set; }
    [JsonPropertyName("Value")] public string Value { get; set; } = "";
}
