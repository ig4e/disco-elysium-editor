using System.Text.Json.Serialization;

namespace DiscoSaveEditor.Models.SaveFile;

/// <summary>
/// Represents the .1st.ntwtf.json save file.
/// Contains party state, current area, and fog of war cache.
/// </summary>
public class FirstFile
{
    [JsonPropertyName("areaId")]
    public string AreaId { get; set; } = "";

    [JsonPropertyName("partyState")]
    public PartyState PartyState { get; set; } = new();

    [JsonPropertyName("fowUnrevealersStatusCache")]
    public Dictionary<string, string> FowUnrevealersStatusCache { get; set; } = new();
}

public class PartyState
{
    [JsonPropertyName("isKimInParty")] public bool IsKimInParty { get; set; }
    [JsonPropertyName("isKimLeftOutside")] public bool IsKimLeftOutside { get; set; }
    [JsonPropertyName("isKimAbandoned")] public bool IsKimAbandoned { get; set; }
    [JsonPropertyName("isKimAwayUpToMorning")] public bool IsKimAwayUpToMorning { get; set; }
    [JsonPropertyName("isKimSleepingInHisRoom")] public bool IsKimSleepingInHisRoom { get; set; }
    [JsonPropertyName("isKimSayingGoodMorning")] public bool IsKimSayingGoodMorning { get; set; }
    [JsonPropertyName("isCunoInParty")] public bool IsCunoInParty { get; set; }
    [JsonPropertyName("isCunoLeftOutside")] public bool IsCunoLeftOutside { get; set; }
    [JsonPropertyName("isCunoAbandoned")] public bool IsCunoAbandoned { get; set; }
    [JsonPropertyName("hasHangover")] public bool HasHangover { get; set; }
    [JsonPropertyName("sleepLocation")] public int SleepLocation { get; set; }
    [JsonPropertyName("waitLocation")] public int WaitLocation { get; set; }
    [JsonPropertyName("cunoWaitLocation")] public int CunoWaitLocation { get; set; }
    [JsonPropertyName("timeSinceKimWentSleepingInHisRoom")] public int TimeSinceKimWentSleepingInHisRoom { get; set; }
    [JsonPropertyName("kimLastArrivalLocation")] public int KimLastArrivalLocation { get; set; }
    [JsonPropertyName("cunoLastArrivalLocation")] public int CunoLastArrivalLocation { get; set; }
}
