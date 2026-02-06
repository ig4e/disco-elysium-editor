using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.SaveFile;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for editing container loot (402 containers with item registries).
/// </summary>
public partial class ContainersViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedContainerId { get; set; } = "";

    public ObservableCollection<ContainerDisplayItem> Containers { get; } = new();
    public ObservableCollection<ContainerItemDisplay> CurrentContainerItems { get; } = new();

    private Dictionary<string, List<ContainerItem>> _containerData = new();

    partial void OnSearchQueryChanged(string value)
    {
        RefreshContainerList();
    }

    partial void OnSelectedContainerIdChanged(string value)
    {
        LoadContainerItems(value);
    }

    public void LoadFromSave(SaveData save)
    {
        _containerData = save.Second.ContainerSourceState.ItemRegistry;
        RefreshContainerList();
    }

    private void RefreshContainerList()
    {
        Containers.Clear();
        var query = SearchQuery?.Trim().ToLowerInvariant() ?? "";

        var filtered = string.IsNullOrEmpty(query)
            ? _containerData
            : _containerData.Where(kv => kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var (containerId, items) in filtered.OrderBy(kv => kv.Key))
        {
            Containers.Add(new ContainerDisplayItem
            {
                ContainerId = containerId,
                ItemCount = items.Count,
                TotalValue = items.Sum(i => i.CalculatedValue)
            });
        }

        if (Containers.Count > 0 && string.IsNullOrEmpty(SelectedContainerId))
        {
            SelectedContainerId = Containers[0].ContainerId;
        }
    }

    private void LoadContainerItems(string containerId)
    {
        CurrentContainerItems.Clear();

        if (string.IsNullOrEmpty(containerId) || !_containerData.ContainsKey(containerId))
            return;

        var items = _containerData[containerId];
        foreach (var item in items)
        {
            CurrentContainerItems.Add(new ContainerItemDisplay
            {
                ContainerId = containerId,
                Name = item.Name,
                Probability = item.Probability,
                Value = item.Value,
                Deviation = item.Deviation,
                CalculatedValue = item.CalculatedValue,
                BonusLoot = item.BonusLoot
            });
        }
    }

    [RelayCommand]
    private void RemoveItem(ContainerItemDisplay? item)
    {
        if (item == null || !_containerData.ContainsKey(item.ContainerId))
            return;

        var container = _containerData[item.ContainerId];
        var toRemove = container.FirstOrDefault(ci =>
            ci.Name == item.Name &&
            Math.Abs(ci.Probability - item.Probability) < 0.001 &&
            ci.Value == item.Value);

        if (toRemove != null)
        {
            container.Remove(toRemove);
            LoadContainerItems(item.ContainerId);
            RefreshContainerList(); // Update counts
        }
    }

    [RelayCommand]
    private void ClearContainer()
    {
        if (string.IsNullOrEmpty(SelectedContainerId) || !_containerData.ContainsKey(SelectedContainerId))
            return;

        _containerData[SelectedContainerId].Clear();
        LoadContainerItems(SelectedContainerId);
        RefreshContainerList();
    }

    public void ApplyToSave(SaveData save)
    {
        save.Second.ContainerSourceState.ItemRegistry = _containerData;
    }
}

public class ContainerDisplayItem
{
    public string ContainerId { get; set; } = "";
    public int ItemCount { get; set; }
    public int TotalValue { get; set; }
}

public partial class ContainerItemDisplay : ObservableObject
{
    public string ContainerId { get; set; } = "";

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial double Probability { get; set; }

    [ObservableProperty]
    public partial int Value { get; set; }

    [ObservableProperty]
    public partial int Deviation { get; set; }

    [ObservableProperty]
    public partial int CalculatedValue { get; set; }

    [ObservableProperty]
    public partial bool BonusLoot { get; set; }
}
