// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class VirusSampleComponent : Component
{
    /// <summary>Exact strain to load into the container.</summary>
    [DataField(required: true)]
    public ProtoId<VirusPrototype> Virus;

    /// <summary>Reagent the virus rides on.</summary>
    [DataField]
    public ProtoId<ReagentPrototype> Carrier = "StableMutagen";

    /// <summary>How much carrier reagent to add.</summary>
    [DataField]
    public FixedPoint2 Amount = 30;

    /// <summary>Solution on this entity to stamp into.</summary>
    [DataField]
    public string Solution = "drink";
}
