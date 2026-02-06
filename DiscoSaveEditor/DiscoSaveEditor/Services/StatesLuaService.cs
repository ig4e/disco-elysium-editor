using System.Text;
using System.Text.RegularExpressions;
using DiscoSaveEditor.Models.SaveFile;

namespace DiscoSaveEditor.Services;

/// <summary>
/// Parses and serializes the .states.lua text file.
/// Contains area progression states and shown interaction orbs.
/// </summary>
public partial class StatesLuaService
{
    [GeneratedRegex(@"AreaState\[""(.+?)""\]\s*=\s*\{LocationState=(\d+)\}")]
    private static partial Regex AreaStateRegex();

    [GeneratedRegex(@"ShownOrbs\[""(.+?)""\]\s*=\s*\{OrbSeen=(\d+)\}")]
    private static partial Regex ShownOrbsRegex();

    public StatesData Parse(string content)
    {
        var data = new StatesData();

        foreach (Match m in AreaStateRegex().Matches(content))
        {
            data.AreaStates[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        }

        foreach (Match m in ShownOrbsRegex().Matches(content))
        {
            data.ShownOrbs[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        }

        return data;
    }

    public string Serialize(StatesData data)
    {
        var sb = new StringBuilder();

        foreach (var (key, val) in data.AreaStates.OrderBy(kv => kv.Key))
        {
            sb.AppendLine($"AreaState[\"{key}\"]={{LocationState={val}}};");
        }

        foreach (var (key, val) in data.ShownOrbs.OrderBy(kv => kv.Key))
        {
            sb.AppendLine($"ShownOrbs[\"{key}\"]={{OrbSeen={val}}};");
        }

        return sb.ToString();
    }
}
