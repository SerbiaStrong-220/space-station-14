// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

[Prototype]
public sealed partial class VirusSymptomRemovalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>Dose spent to strip one symptom.</summary>
    [DataField]
    public FixedPoint2 Amount = 5;

    [DataField]
    public SoundSpecifier? RemoveSound;
}
