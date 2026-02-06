using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.GameData;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Inventory page.
/// Shows all items, which are owned and equipped. Supports search and item catalog browsing.
/// </summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly GameDataService _gameData;
    private readonly List<InventoryDisplayItem> _allOwnedItems = new();

    public ObservableCollection<InventoryDisplayItem> OwnedItems { get; } = new();
    public ObservableCollection<InventoryDisplayItem> AllGameItems { get; } = new();
    public ObservableCollection<InventoryDisplayItem> FilteredCatalog { get; } = new();

    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial string CatalogSearchQuery { get; set; }
    [ObservableProperty] public partial int Bullets { get; set; }

    public InventoryViewModel(GameDataService gameData)
    {
        _gameData = gameData;
        SearchQuery = "";
        CatalogSearchQuery = "";
    }

    partial void OnSearchQueryChanged(string value)
    {
        RefreshOwnedDisplay();
    }

    partial void OnCatalogSearchQueryChanged(string value)
    {
        RefreshCatalogDisplay();
    }

    private void RefreshOwnedDisplay()
    {
        OwnedItems.Clear();
        var q = SearchQuery?.ToLower() ?? "";
        foreach (var item in _allOwnedItems)
        {
            if (!string.IsNullOrEmpty(q) &&
                !item.DisplayName.ToLower().Contains(q) &&
                !item.Name.ToLower().Contains(q))
                continue;
            OwnedItems.Add(item);
        }
    }

    private void RefreshCatalogDisplay()
    {
        FilteredCatalog.Clear();
        var q = CatalogSearchQuery?.ToLower() ?? "";
        var ownedNames = new HashSet<string>(_allOwnedItems.Select(i => i.Name));

        foreach (var item in AllGameItems)
        {
            if (ownedNames.Contains(item.Name)) continue; // Hide already owned
            if (!string.IsNullOrEmpty(q) &&
                !item.DisplayName.ToLower().Contains(q) &&
                !item.Name.ToLower().Contains(q))
                continue;
            FilteredCatalog.Add(item);
        }
    }

    public void LoadFromSave(SaveData save)
    {
        var cs = save.Second.CharacterSheet;
        var invState = save.Second.InventoryState;

        Bullets = invState.InventoryViewState.Bullets;

        // Build owned items list
        _allOwnedItems.Clear();
        foreach (var itemName in cs.GainedItems)
        {
            var gameDef = _gameData.GetItem(itemName);
            var state = invState.ItemListState.FirstOrDefault(i => i.ItemName == itemName);
            var isEquipped = cs.EquippedItems.Contains(itemName);

            // Find equipment slot
            var slot = invState.InventoryViewState.Equipment
                .FirstOrDefault(kv => kv.Value == itemName).Key ?? "";

            _allOwnedItems.Add(new InventoryDisplayItem
            {
                Name = itemName,
                DisplayName = gameDef?.DisplayName ?? itemName,
                Description = gameDef?.Description ?? "",
                Bonus = gameDef?.MediumTextValue ?? "",
                IsOwned = true,
                IsEquipped = isEquipped,
                EquipSlot = slot,
                IsQuestItem = gameDef?.IsQuestItem ?? false,
                IsCursed = gameDef?.IsCursed ?? false,
                IsSubstance = gameDef?.IsSubstanceItem ?? false,
                SubstanceUses = state?.SubstanceUses ?? 0
            });
        }

        // Build full game item catalog
        AllGameItems.Clear();
        foreach (var item in _gameData.Items.Values.OrderBy(i => i.DisplayName))
        {
            AllGameItems.Add(new InventoryDisplayItem
            {
                Name = item.Name,
                DisplayName = item.DisplayName,
                Description = item.Description,
                Bonus = item.MediumTextValue,
                IsOwned = false,
                IsEquipped = false,
                IsQuestItem = item.IsQuestItem,
                IsCursed = item.IsCursed,
                IsSubstance = item.IsSubstanceItem
            });
        }

        RefreshOwnedDisplay();
        RefreshCatalogDisplay();
    }

    public void ApplyToSave(SaveData save)
    {
        var cs = save.Second.CharacterSheet;
        var ivs = save.Second.InventoryState.InventoryViewState;

        cs.GainedItems = _allOwnedItems.Where(i => i.IsOwned).Select(i => i.Name).ToList();
        cs.EquippedItems = _allOwnedItems.Where(i => i.IsEquipped).Select(i => i.Name).ToList();

        // Rebuild equipment map (slot → item name)
        ivs.Equipment.Clear();
        foreach (var item in _allOwnedItems.Where(i => i.IsEquipped && !string.IsNullOrEmpty(i.EquipSlot)))
        {
            ivs.Equipment[item.EquipSlot] = item.Name;
        }

        ivs.Bullets = Bullets;
    }

    [RelayCommand]
    private void AddItem(InventoryDisplayItem item)
    {
        if (!_allOwnedItems.Any(i => i.Name == item.Name))
        {
            var owned = new InventoryDisplayItem
            {
                Name = item.Name,
                DisplayName = item.DisplayName,
                Description = item.Description,
                Bonus = item.Bonus,
                IsOwned = true,
                IsEquipped = false,
                IsQuestItem = item.IsQuestItem,
                IsCursed = item.IsCursed,
                IsSubstance = item.IsSubstance
            };
            _allOwnedItems.Add(owned);
            RefreshOwnedDisplay();
            RefreshCatalogDisplay();
        }
    }

    [RelayCommand]
    private void RemoveItem(InventoryDisplayItem item)
    {
        _allOwnedItems.Remove(item);
        RefreshOwnedDisplay();
        RefreshCatalogDisplay();
    }

    [RelayCommand]
    private void ToggleEquip(InventoryDisplayItem item)
    {
        item.IsEquipped = !item.IsEquipped;
    }

    [RelayCommand]
    private void AddAllItems()
    {
        foreach (var gameItem in AllGameItems)
        {
            if (!_allOwnedItems.Any(i => i.Name == gameItem.Name))
            {
                _allOwnedItems.Add(new InventoryDisplayItem
                {
                    Name = gameItem.Name,
                    DisplayName = gameItem.DisplayName,
                    Description = gameItem.Description,
                    Bonus = gameItem.Bonus,
                    IsOwned = true,
                    IsEquipped = false,
                    IsQuestItem = gameItem.IsQuestItem,
                    IsCursed = gameItem.IsCursed,
                    IsSubstance = gameItem.IsSubstance
                });
            }
        }
        RefreshOwnedDisplay();
        RefreshCatalogDisplay();
    }

    [RelayCommand]
    private void RemoveAllItems()
    {
        foreach (var item in _allOwnedItems.ToList())
        {
            if (!item.IsQuestItem)
                _allOwnedItems.Remove(item);
        }
        RefreshOwnedDisplay();
        RefreshCatalogDisplay();
    }
}

public partial class InventoryDisplayItem : ObservableObject
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Bonus { get; set; } = "";
    public string EquipSlot { get; set; } = "";
    public bool IsQuestItem { get; set; }
    public bool IsCursed { get; set; }
    public bool IsSubstance { get; set; }
    public int SubstanceUses { get; set; }

    [ObservableProperty] public partial bool IsOwned { get; set; }
    [ObservableProperty] public partial bool IsEquipped { get; set; }
}
