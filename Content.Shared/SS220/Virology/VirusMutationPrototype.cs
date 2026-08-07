// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

[Prototype]
public sealed partial class VirusMutationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Reagent that triggers mutation.</summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Mutagen;

    /// <summary>Symptoms that can be mutated in.</summary>
    [DataField(required: true)]
    public List<ProtoId<VirusSymptomPrototype>> Pool = [];

    /// <summary>Dose spent per mutation.</summary>
    [DataField]
    public FixedPoint2 Cost = 1;

    [DataField]
    public SoundSpecifier? MutateSound;
}
