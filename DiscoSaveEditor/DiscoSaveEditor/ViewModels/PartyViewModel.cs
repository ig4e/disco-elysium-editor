using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DiscoSaveEditor.Models.SaveFile;

namespace DiscoSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Party page.
/// Controls Kim/Cuno party state, area, HUD, game mode, and location flags.
/// </summary>
public partial class PartyViewModel : ObservableObject
{
    [ObservableProperty] public partial string AreaId { get; set; }
    [ObservableProperty] public partial bool IsKimInParty { get; set; }
    [ObservableProperty] public partial bool IsKimLeftOutside { get; set; }
    [ObservableProperty] public partial bool IsKimAbandoned { get; set; }
    [ObservableProperty] public partial bool IsKimAwayUpToMorning { get; set; }
    [ObservableProperty] public partial bool IsKimSleepingInHisRoom { get; set; }
    [ObservableProperty] public partial bool IsCunoInParty { get; set; }
    [ObservableProperty] public partial bool IsCunoLeftOutside { get; set; }
    [ObservableProperty] public partial bool IsCunoAbandoned { get; set; }
    [ObservableProperty] public partial bool HasHangover { get; set; }

    // HUD / Portrait
    [ObservableProperty] public partial bool PortraitObscured { get; set; }
    [ObservableProperty] public partial bool PortraitShaved { get; set; }
    [ObservableProperty] public partial bool PortraitFascist { get; set; }

    // Game Mode
    [ObservableProperty] public partial string GameMode { get; set; }
    public ObservableCollection<string> GameModes { get; } = new() { "NORMAL", "HARDCORE" };

    // Location flags
    [ObservableProperty] public partial bool WasChurchVisited { get; set; }
    [ObservableProperty] public partial bool WasFishingVillageVisited { get; set; }
    [ObservableProperty] public partial bool WasQuicktravelChurchDiscovered { get; set; }
    [ObservableProperty] public partial bool WasQuicktravelFishingVillageDiscovered { get; set; }

    public PartyViewModel()
    {
        AreaId = "";
        GameMode = "NORMAL";
    }

    public void LoadFromSave(SaveData save)
    {
        AreaId = save.First.AreaId;
        IsKimInParty = save.First.PartyState.IsKimInParty;
        IsKimLeftOutside = save.First.PartyState.IsKimLeftOutside;
        IsKimAbandoned = save.First.PartyState.IsKimAbandoned;
        IsKimAwayUpToMorning = save.First.PartyState.IsKimAwayUpToMorning;
        IsKimSleepingInHisRoom = save.First.PartyState.IsKimSleepingInHisRoom;
        IsCunoInParty = save.First.PartyState.IsCunoInParty;
        IsCunoLeftOutside = save.First.PartyState.IsCunoLeftOutside;
        IsCunoAbandoned = save.First.PartyState.IsCunoAbandoned;
        HasHangover = save.First.PartyState.HasHangover;

        // HUD state
        PortraitObscured = save.Second.HudState.TequilaPortraitObscured;
        PortraitShaved = save.Second.HudState.TequilaPortraitShaved;
        PortraitFascist = save.Second.HudState.TequilaPortraitFascist;

        // Game mode
        GameMode = save.Second.GameModeState.GameMode;

        // Location flags
        WasChurchVisited = save.Second.AcquiredJournalTasks.WasChurchVisited;
        WasFishingVillageVisited = save.Second.AcquiredJournalTasks.WasFishingVillageVisited;
        WasQuicktravelChurchDiscovered = save.Second.AcquiredJournalTasks.WasQuicktravelChurchDiscovered;
        WasQuicktravelFishingVillageDiscovered = save.Second.AcquiredJournalTasks.WasQuicktravelFishingVillageDiscovered;
    }

    public void ApplyToSave(SaveData save)
    {
        save.First.AreaId = AreaId;
        save.First.PartyState.IsKimInParty = IsKimInParty;
        save.First.PartyState.IsKimLeftOutside = IsKimLeftOutside;
        save.First.PartyState.IsKimAbandoned = IsKimAbandoned;
        save.First.PartyState.IsKimAwayUpToMorning = IsKimAwayUpToMorning;
        save.First.PartyState.IsKimSleepingInHisRoom = IsKimSleepingInHisRoom;
        save.First.PartyState.IsCunoInParty = IsCunoInParty;
        save.First.PartyState.IsCunoLeftOutside = IsCunoLeftOutside;
        save.First.PartyState.IsCunoAbandoned = IsCunoAbandoned;
        save.First.PartyState.HasHangover = HasHangover;

        // HUD
        save.Second.HudState.TequilaPortraitObscured = PortraitObscured;
        save.Second.HudState.TequilaPortraitShaved = PortraitShaved;
        save.Second.HudState.TequilaPortraitFascist = PortraitFascist;

        // Game mode
        save.Second.GameModeState.GameMode = GameMode;

        // Location flags
        save.Second.AcquiredJournalTasks.WasChurchVisited = WasChurchVisited;
        save.Second.AcquiredJournalTasks.WasFishingVillageVisited = WasFishingVillageVisited;
        save.Second.AcquiredJournalTasks.WasQuicktravelChurchDiscovered = WasQuicktravelChurchDiscovered;
        save.Second.AcquiredJournalTasks.WasQuicktravelFishingVillageDiscovered = WasQuicktravelFishingVillageDiscovered;
    }
}
