// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class VirusMutationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Reagent that drives the mutation when it contacts a virus.</summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Mutagen;

    /// <summary>Symptoms this solution can add.</summary>
    [DataField(required: true)]
    public List<ProtoId<PathologyPrototype>> Pool = new();

    /// <summary>Played in the container when a mutation happen.</summary>
    [DataField]
    public SoundSpecifier MutateSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}
