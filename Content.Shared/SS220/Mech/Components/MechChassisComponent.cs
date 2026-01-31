using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Mech.Components;

/// <summary>
/// Chassis(a.k.a.legs) of the mech
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechChassisComponent : Component
{
    /// <summary>
    /// How much the mech can carry
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 MaximalMass = 0;

    /// <summary>
    /// Movement speed this chassis provides
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float BaseMovementSpeed = 1f;

    /// <summary>
    /// Acceleration this chassis provides
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Acceleration = 1f;
}
