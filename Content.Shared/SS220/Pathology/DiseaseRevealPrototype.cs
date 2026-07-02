// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class DiseaseRevealPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Reagent that performs the reveal.</summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>Genome whose symptoms this reagent can reveal.</summary>
    [DataField(required: true)]
    public VirusGenome Genome;

    /// <summary>Reagent spent per reveal attempt.</summary>
    [DataField]
    public FixedPoint2 Amount = 5;

    /// <summary>Sound played in the container when a symptom is revealed.</summary>
    [DataField]
    public SoundSpecifier RevealSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}
