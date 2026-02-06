using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.SaveFile;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for editing door states and area/orb states.
/// </summary>
public partial class StatesViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string DoorSearchQuery { get; set; } = "";

    [ObservableProperty]
    public partial string AreaSearchQuery { get; set; } = "";

    [ObservableProperty]
    public partial string OrbSearchQuery { get; set; } = "";

    public ObservableCollection<DoorStateItem> Doors { get; } = new();
    public ObservableCollection<AreaStateItem> AreaStates { get; } = new();
    public ObservableCollection<OrbStateItem> ShownOrbs { get; } = new();

    private Dictionary<string, bool> _doorStates = new();
    private Dictionary<string, int> _areaStates = new();
    private Dictionary<string, int> _shownOrbs = new();

    partial void OnDoorSearchQueryChanged(string value) => RefreshDoorList();
    partial void OnAreaSearchQueryChanged(string value) => RefreshAreaList();
    partial void OnOrbSearchQueryChanged(string value) => RefreshOrbList();

    public void LoadFromSave(SaveData save)
    {
        _doorStates = save.Second.VariousItemsHolder.DoorStates;
        _areaStates = save.States.AreaStates;
        _shownOrbs = save.States.ShownOrbs;

        RefreshDoorList();
        RefreshAreaList();
        RefreshOrbList();
    }

    private void RefreshDoorList()
    {
        Doors.Clear();
        var query = DoorSearchQuery?.Trim().ToLowerInvariant() ?? "";

        var filtered = string.IsNullOrEmpty(query)
            ? _doorStates
            : _doorStates.Where(kv => kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var (doorId, isOpen) in filtered.OrderBy(kv => kv.Key))
        {
            Doors.Add(new DoorStateItem
            {
                DoorId = doorId,
                IsOpen = isOpen
            });
        }
    }

    private void RefreshAreaList()
    {
        AreaStates.Clear();
        var query = AreaSearchQuery?.Trim().ToLowerInvariant() ?? "";

        var filtered = string.IsNullOrEmpty(query)
            ? _areaStates
            : _areaStates.Where(kv => kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var (areaId, state) in filtered.OrderBy(kv => kv.Key))
        {
            AreaStates.Add(new AreaStateItem
            {
                AreaId = areaId,
                LocationState = state
            });
        }
    }

    private void RefreshOrbList()
    {
        ShownOrbs.Clear();
        var query = OrbSearchQuery?.Trim().ToLowerInvariant() ?? "";

        var filtered = string.IsNullOrEmpty(query)
            ? _shownOrbs
            : _shownOrbs.Where(kv => kv.Key.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var (orbId, seen) in filtered.OrderBy(kv => kv.Key))
        {
            ShownOrbs.Add(new OrbStateItem
            {
                OrbId = orbId,
                OrbSeen = seen
            });
        }
    }

    [RelayCommand]
    private void OpenAllDoors()
    {
        foreach (var key in _doorStates.Keys.ToList())
            _doorStates[key] = true;
        RefreshDoorList();
    }

    [RelayCommand]
    private void CloseAllDoors()
    {
        foreach (var key in _doorStates.Keys.ToList())
            _doorStates[key] = false;
        RefreshDoorList();
    }

    [RelayCommand]
    private void ResetAllAreas()
    {
        foreach (var key in _areaStates.Keys.ToList())
            _areaStates[key] = 0;
        RefreshAreaList();
    }

    [RelayCommand]
    private void ResetAllOrbs()
    {
        foreach (var key in _shownOrbs.Keys.ToList())
            _shownOrbs[key] = 0;
        RefreshOrbList();
    }

    public void ApplyToSave(SaveData save)
    {
        // Apply door states
        save.Second.VariousItemsHolder.DoorStates = _doorStates;

        // Apply area/orb states
        save.States.AreaStates = _areaStates;
        save.States.ShownOrbs = _shownOrbs;
    }

    private void CaptureDoorEdits()
    {
        foreach (var door in Doors)
        {
            if (_doorStates.ContainsKey(door.DoorId))
                _doorStates[door.DoorId] = door.IsOpen;
        }
    }

    private void CaptureAreaEdits()
    {
        foreach (var area in AreaStates)
        {
            if (_areaStates.ContainsKey(area.AreaId))
                _areaStates[area.AreaId] = area.LocationState;
        }
    }

    private void CaptureOrbEdits()
    {
        foreach (var orb in ShownOrbs)
        {
            if (_shownOrbs.ContainsKey(orb.OrbId))
                _shownOrbs[orb.OrbId] = orb.OrbSeen;
        }
    }
}

public partial class DoorStateItem : ObservableObject
{
    public string DoorId { get; set; } = "";

    [ObservableProperty]
    public partial bool IsOpen { get; set; }
}

public partial class AreaStateItem : ObservableObject
{
    public string AreaId { get; set; } = "";

    [ObservableProperty]
    public partial int LocationState { get; set; }
}

public partial class OrbStateItem : ObservableObject
{
    public string OrbId { get; set; } = "";

    [ObservableProperty]
    public partial int OrbSeen { get; set; }
}
