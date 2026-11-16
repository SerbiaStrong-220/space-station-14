// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SS220SharedTriggers.InjectionOnTrigger;

/// <summary>
/// This is used for injects reagents when a trigger is activated
/// </summary>
[RegisterComponent]
public sealed partial class InjectionOnTriggerComponent : Component
{
    /// <summary>
    /// Reagent to inject into the user.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? Reagent;

    /// <summary>
    /// How much of the reagent to inject.
    /// </summary>
    [DataField]
    public FixedPoint2 Quantity;
}
