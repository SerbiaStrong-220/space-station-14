// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.GuideGenerator;

public sealed class StationsJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var configurationManager = IoCManager.Resolve<IConfigurationManager>();

        var mapPool = configurationManager.GetCVar(CCVars.GameMapPool);

        if (!prototypeManager.TryIndex<GameMapPoolPrototype>(mapPool, out var poolPrototype))
        {
            Logger.Error($"Provided map pool prototype \"{mapPool}\" is invalid");
            return;
        }

        var stations = poolPrototype.Maps
            .Select(x => new StationEntry(prototypeManager.Index<GameMapPrototype>(x)))
            .ToList();

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        file.Write(JsonSerializer.Serialize(stations, serializeOptions));
    }

    private sealed class StationEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("image")]
        public string Image { get; }

        public StationEntry(GameMapPrototype proto)
        {
            Id = proto.ID;
            Name = proto.MapName;
            // Note: image path may differ, depending on your way to pass arguments to map renderer
            // current version passes using map prototypes, so proto.ID is used
            Image = "/" + proto.ID + "-0.png";
        }
    }
}
