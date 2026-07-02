// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Weapons.Ranged.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemExtensionRangedWeaponComponent : Component //These values (multiplied by a coefficient between 0 and 1) will be added to the gun if we have enough strength
{
    [DataField]
    public Angle AngleIncrease = Angle.FromDegrees(0);

    [ViewVariables(VVAccess.ReadWrite), DataField("minAngle")]
    public Angle MinAngle = Angle.FromDegrees(0);

    [ViewVariables(VVAccess.ReadWrite), DataField("maxAngle")]
    public Angle MaxAngle = Angle.FromDegrees(0);

    [DataField]
    public Angle AngleDecay = Angle.FromDegrees(0);

    [DataField, AutoNetworkedField]
    public EntityUid? User;
}
