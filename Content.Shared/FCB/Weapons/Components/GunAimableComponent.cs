// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.FCB.Weapons.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class GunAimableComponent : Component//This component signalises that the gun can be used for aimed shooting
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsAimed = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("minAngle")]
    public Angle MinAngle = Angle.FromDegrees(0);

    /// <summary>
    /// Angle bonus applied upon being aimed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxAngle")]
    public Angle MaxAngle = Angle.FromDegrees(0);

    /// <summary>
    /// Recoil bonuses applied upon being aimed.
    /// Higher angle decay bonus, quicker recovery.
    /// Lower angle increase bonus (negative numbers), slower buildup.
    /// </summary>
    [DataField]
    public Angle AngleDecay = Angle.FromDegrees(0);

    /// <summary>
    /// Recoil bonuses applied upon being aimed.
    /// Higher angle decay bonus, quicker recovery.
    /// Lower angle increase bonus (negative numbers), slower buildup.
    /// </summary>
    [DataField]
    public Angle AngleIncrease = Angle.FromDegrees(0);

    [DataField]
    public float? AimedSprintSpeedModifier = 0.7f;

    [DataField]
    public float? AimedWalkingSpeedModifier = 1f;
}
