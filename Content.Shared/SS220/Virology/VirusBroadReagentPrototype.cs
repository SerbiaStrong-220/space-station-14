// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

[Prototype]
public sealed partial class VirusBroadReagentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>What metabolising this reagent does to every strain on host.</summary>
    [DataField]
    public VirusBroadAction Action = VirusBroadAction.Suppress;

    /// <summary>Dose required in the carrier's blood for the effect.</summary>
    [DataField]
    public FixedPoint2 Amount = 5;
}

/// <summary>What a broadreagent does to the carrier's strains.</summary>
public enum VirusBroadAction : byte
{
    /// <summary>Fully deletes every virus.</summary>
    Cure,

    /// <summary>Suppreses every virus.</summary>
    Suppress,
}
