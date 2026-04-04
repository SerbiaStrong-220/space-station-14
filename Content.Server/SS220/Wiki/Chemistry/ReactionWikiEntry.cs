// Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/GuideGenerator/ReagentEntry.cs
using Content.Server.GuideGenerator;
using Content.Shared.Chemistry.Reaction;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.SS220.Wiki.Chemistry;

public sealed class ReactionWikiEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("reactants")]
    public Dictionary<string, ReactantEntry> Reactants { get; }

    [JsonPropertyName("products")]
    public Dictionary<string, float> Products { get; }

    [JsonPropertyName("mixingCategories")]
    public List<MixingCategoryWikiEntry> MixingCategories { get; } = new();

    [JsonPropertyName("minTemp")]
    public float MinTemp { get; }

    [JsonPropertyName("maxTemp")]
    public float MaxTemp { get; }

    [JsonPropertyName("hasMax")]
    public bool HasMax { get; }

    [JsonPropertyName("effects")]
    public List<EntityEffectWikiEntry> Effects { get; } = new();

    public ReactionWikiEntry(ReactionPrototype proto)
    {
        Id = proto.ID;
        Name = CapitalizeFirstLetter(proto.Name);
        Reactants =
            proto.Reactants
                .Select(x => KeyValuePair.Create(x.Key, new ReactantEntry(x.Value.Amount.Float(), x.Value.Catalyst)))
                .ToDictionary(x => x.Key, x => x.Value);
        Products =
            proto.Products
                .Select(x => KeyValuePair.Create(x.Key, x.Value.Float()))
                .ToDictionary(x => x.Key, x => x.Value);

        Effects = proto.Effects.Select(x => new EntityEffectWikiEntry(x)).ToList();
        MinTemp = proto.MinimumTemperature;
        MaxTemp = proto.MaximumTemperature;
        HasMax = !float.IsPositiveInfinity(MaxTemp);
    }

    private string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var firstChar = char.ToUpper(input[0]);
        return firstChar + input.Substring(1);
    }
}
