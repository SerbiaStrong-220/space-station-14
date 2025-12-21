// Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/Corvax/GuideGenerator/ReagentEffectsEntry.cs
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.SS220.Wiki.Chemistry;

public sealed class ReagentEffectsWikiEntry
{
    [JsonPropertyName("rate")]
    public FixedPoint2 MetabolismRate { get; } = FixedPoint2.New(0.5f);

    [JsonPropertyName("effects")]
    public List<EntityEffectWikiEntry> Effects { get; } = new();

    public ReagentEffectsWikiEntry(ReagentEffectsEntry entry)
    {
        MetabolismRate = entry.MetabolismRate;
        Effects = entry.Effects.Select(x => new EntityEffectWikiEntry(x)).ToList();
    }
}
