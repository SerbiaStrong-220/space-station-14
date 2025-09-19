// Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/GuideGenerator/ReagentEntry.cs
using Content.Shared.Chemistry.Reagent;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.SS220.Wiki.Chemistry;

public sealed class ReagentWikiEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("group")]
    public string Group { get; }

    [JsonPropertyName("desc")]
    public string Description { get; }

    [JsonPropertyName("physicalDesc")]
    public string PhysicalDescription { get; }

    [JsonPropertyName("color")]
    public string SubstanceColor { get; }

    [JsonPropertyName("textColor")]
    public string TextColor { get; }

    [JsonPropertyName("recipes")]
    public List<string> Recipes { get; } = new();

    [JsonPropertyName("metabolisms")]
    public Dictionary<string, ReagentEffectsWikiEntry>? Metabolisms { get; }

    public ReagentWikiEntry(ReagentPrototype prototype)
    {
        Id = prototype.ID;
        Name = prototype.LocalizedName;
        Group = prototype.Group;
        Description = prototype.LocalizedDescription;
        PhysicalDescription = prototype.LocalizedPhysicalDescription;
        SubstanceColor = prototype.SubstanceColor.ToHex();
        TextColor = GetTextColor(prototype);
        Metabolisms = prototype.Metabolisms?.AsReadOnly().ToDictionary(x => x.Key.Id, x => new ReagentEffectsWikiEntry(x.Value));
    }

    private static string GetTextColor(ReagentPrototype reagentPrototype)
    {
        var r = reagentPrototype.SubstanceColor.R;
        var g = reagentPrototype.SubstanceColor.G;
        var b = reagentPrototype.SubstanceColor.B;

        var luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
        return (luminance > 0.5 ? Color.Black : Color.White).ToHex();
    }
}
