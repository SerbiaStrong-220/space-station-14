// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MartialArts.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts;

[Prototype]
public sealed partial class MartialArtPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public CombatSequence[] Sequences = [];

    [DataField]
    public HashSet<MartialArtEffect> Effects = [];
}
