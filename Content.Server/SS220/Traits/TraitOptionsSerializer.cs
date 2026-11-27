// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Text.Json;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.SS220.Traits;

/// <summary>
/// Serializes selected trait options of a character profile into a JSON string for database storage.
/// </summary>
public static class TraitOptionsSerializer
{
    public static string? Serialize(IReadOnlyDictionary<ProtoId<TraitPrototype>, string> options)
    {
        if (options.Count == 0)
            return null;

        return JsonSerializer.Serialize(options.ToDictionary(o => o.Key.Id, o => o.Value));
    }

    public static Dictionary<ProtoId<TraitPrototype>, string> Deserialize(string? json)
    {
        var result = new Dictionary<ProtoId<TraitPrototype>, string>();

        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (raw == null)
                return result;

            foreach (var (traitId, option) in raw)
            {
                result[traitId] = option;
            }
        }
        catch (JsonException e)
        {
            Log.Warning($"Failed to deserialize trait options: {e.Message}");
        }

        return result;
    }
}

