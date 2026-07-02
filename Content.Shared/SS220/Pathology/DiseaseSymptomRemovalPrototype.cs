// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class DiseaseSymptomRemovalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Reagent that performs the removal.</summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>Reagent spent per symptom removed.</summary>
    [DataField]
    public FixedPoint2 Amount = 10;

    /// <summary>Played in the container when a symptom is removed.</summary>
    [DataField]
    public SoundSpecifier RemoveSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}
