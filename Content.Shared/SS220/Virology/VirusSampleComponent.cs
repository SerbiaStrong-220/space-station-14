// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

/// <summary>Stamps this entity's solution with a strain descriptor, so container carries a virus sample.</summary>
[RegisterComponent]
public sealed partial class VirusSampleComponent : Component
{
    /// <summary>Exact strain to load into container.</summary>
    [DataField(required: true)]
    public ProtoId<VirusPrototype> Virus;

    /// <summary>Reagent virus being carried on.</summary>
    [DataField]
    public ProtoId<ReagentPrototype> Carrier = "StableMutagen";

    /// <summary>How much carrier reagent to add.</summary>
    [DataField]
    public FixedPoint2 Amount = 15;

    /// <summary>Solution to stamp into.</summary>
    [DataField]
    public string Solution = "drink";
}
