using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscoSaveEditor.Models.SaveFile;
using DiscoSaveEditor.Services;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// Root ViewModel — manages save loading/saving, navigation state, and child VMs.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly GameDataService _gameData;
    private readonly SaveFileService _saveService;

    public UndoRedoService UndoRedo { get; } = new();

    public ObservableCollection<RecentSaveItem> RecentSaves { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSaveLoaded))]
    [NotifyPropertyChangedFor(nameof(ShowWelcome))]
    [NotifyPropertyChangedFor(nameof(SaveDisplayName))]
    public partial SaveData? CurrentSave { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; }

    // Child ViewModels
    public CharacterViewModel Character { get; }
    public InventoryViewModel Inventory { get; }
    public ThoughtCabinetViewModel ThoughtCabinet { get; }
    public JournalViewModel Journal { get; }
    public PartyViewModel Party { get; }
    public WorldViewModel World { get; }
    public WhiteChecksViewModel WhiteChecks { get; }
    public ContainersViewModel Containers { get; }
    public StatesViewModel States { get; }

    public bool IsSaveLoaded => CurrentSave != null;
    public bool ShowWelcome => CurrentSave == null;
    public string SaveDisplayName => CurrentSave?.BaseName ?? "No Save Loaded";

    public MainViewModel(GameDataService gameData)
    {
        _gameData = gameData;
        _saveService = new SaveFileService(gameData);

        StatusMessage = "Looking for saves...";

        Character = new CharacterViewModel(gameData);
        Inventory = new InventoryViewModel(gameData);
        ThoughtCabinet = new ThoughtCabinetViewModel(gameData);
        Journal = new JournalViewModel(gameData);
        Party = new PartyViewModel();
        World = new WorldViewModel(gameData);
        WhiteChecks = new WhiteChecksViewModel(gameData);
        Containers = new ContainersViewModel();
        States = new StatesViewModel();

        // Update command states when undo/redo state changes
        UndoRedo.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UndoRedo.CanUndo))
                UndoCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(UndoRedo.CanRedo))
                RedoCommand.NotifyCanExecuteChanged();
        };

        _ = RefreshDiscoveredSavesAsync();
    }

    [RelayCommand]
    public async Task RefreshDiscoveredSavesAsync()
    {
        RecentSaves.Clear();
        
        var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var possiblePaths = new[]
        {
            Path.Combine(userPath, "AppData", "LocalLow", "ZAUM Studio", "Disco Elysium", "SaveGames"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Disco Elysium", "SaveGames"),
            Path.Combine(userPath, "AppData", "LocalLow", "ZA-UM", "Disco Elysium", "SaveGames"),
            Path.Combine(userPath, "AppData", "LocalLow", "ZA-UM", "Disco Elysium - The Final Cut", "SaveGames"),
            Path.Combine(userPath, "AppData", "LocalLow", "ZAUM Studio", "Disco Elysium - The Final Cut", "SaveGames"),
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                var directories = Directory.GetDirectories(path, "*.ntwtf");
                foreach (var dir in directories.OrderByDescending(Directory.GetLastWriteTime))
                {
                    if (RecentSaves.Any(s => s.Path == dir)) continue;
                    
                    RecentSaves.Add(new RecentSaveItem 
                    { 
                        Name = Path.GetFileName(dir), 
                        Path = dir,
                        LastModified = Directory.GetLastWriteTime(dir)
                    });
                }
            }
        }

        if (RecentSaves.Count > 0)
            StatusMessage = $"Found {RecentSaves.Count} save{(RecentSaves.Count == 1 ? "" : "s")}. Click one to load it.";
        else
            StatusMessage = "No saves found. Use 'Open Save Folder' to browse manually.";
    }

    [RelayCommand]
    private async Task OpenSaveAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowHelper.GetActiveWindowHandle();
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            await LoadSaveAsync(folder.Path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening save: {ex.Message}";
        }
    }

    public async Task LoadSaveAsync(string folderPath)
    {
        IsBusy = true;
        StatusMessage = "Loading save...";

        try
        {
            if (!_gameData.IsLoaded)
            {
                var gameDataPath = Path.Combine(AppContext.BaseDirectory, "Assets", "GameData");
                if (!Directory.Exists(gameDataPath))
                {
                    gameDataPath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, "..", "..", "..", "..", "game_assets");
                }
                await _gameData.LoadAsync(gameDataPath);
            }

            CurrentSave = await _saveService.LoadAsync(folderPath);

            // Populate child viewmodels
            Character.LoadFromSave(CurrentSave);
            Inventory.LoadFromSave(CurrentSave);
            ThoughtCabinet.LoadFromSave(CurrentSave);
            Journal.LoadFromSave(CurrentSave);
            Party.LoadFromSave(CurrentSave);
            World.LoadFromSave(CurrentSave);
            WhiteChecks.LoadFromSave(CurrentSave);
            Containers.LoadFromSave(CurrentSave);
            States.LoadFromSave(CurrentSave);

            UndoRedo.Clear();
            HasUnsavedChanges = false;
            StatusMessage = $"Loaded: {CurrentSave.BaseName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading save: {ex.Message}";
            CurrentSave = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentSave == null) return;

        IsBusy = true;
        StatusMessage = "Saving...";

        try
        {
            ApplyAllViewModels();
            await _saveService.SaveAsync(CurrentSave);

            HasUnsavedChanges = false;
            StatusMessage = $"Saved: {CurrentSave.BaseName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (CurrentSave == null) return;

        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowHelper.GetActiveWindowHandle();
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            IsBusy = true;
            StatusMessage = "Saving copy...";

            // Copy all files to the target folder
            var targetPath = Path.Combine(folder.Path, Path.GetFileName(CurrentSave.FolderPath));
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            foreach (var file in Directory.GetFiles(CurrentSave.FolderPath))
            {
                File.Copy(file, Path.Combine(targetPath, Path.GetFileName(file)), overwrite: true);
            }

            // Update save data to point to new location and save
            var originalPath = CurrentSave.FolderPath;
            CurrentSave.FolderPath = targetPath;
            ApplyAllViewModels();
            await _saveService.SaveAsync(CurrentSave);
            CurrentSave.FolderPath = originalPath; // Restore to original

            HasUnsavedChanges = false;
            StatusMessage = $"Saved copy to: {targetPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving copy: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndoCommand))]
    private void Undo()
    {
        UndoRedo.Undo();
        HasUnsavedChanges = true;
    }

    private bool CanUndoCommand() => UndoRedo.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedoCommand))]
    private void Redo()
    {
        UndoRedo.Redo();
        HasUnsavedChanges = true;
    }

    private bool CanRedoCommand() => UndoRedo.CanRedo;

    private void ApplyAllViewModels()
    {
        if (CurrentSave == null) return;
        Character.ApplyToSave(CurrentSave);
        Inventory.ApplyToSave(CurrentSave);
        ThoughtCabinet.ApplyToSave(CurrentSave);
        Journal.ApplyToSave(CurrentSave);
        Party.ApplyToSave(CurrentSave);
        World.ApplyToSave(CurrentSave);
        WhiteChecks.ApplyToSave(CurrentSave);
        Containers.ApplyToSave(CurrentSave);
        States.ApplyToSave(CurrentSave);
    }

    public void MarkDirty()
    {
        HasUnsavedChanges = true;
    }
}

public class RecentSaveItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime LastModified { get; set; }

    public string DisplayName => Name.Replace(".ntwtf", "");
    public string LastModifiedText => LastModified.ToString("MMM d, yyyy  h:mm tt");
}

/// <summary>Helper to get the native window handle for file pickers.</summary>
public static class WindowHelper
{
    public static nint ActiveWindowHandle { get; set; }
    public static nint GetActiveWindowHandle() => ActiveWindowHandle;
}
