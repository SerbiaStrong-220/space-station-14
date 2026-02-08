// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Mech.Parts.Components;

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
    /// How much energy the mech will consume per kilogram of mass it carries
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Efficiency = 1;

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

    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? FootstepSound;
}
