// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.FCB.Mech.Parts.Components;

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
