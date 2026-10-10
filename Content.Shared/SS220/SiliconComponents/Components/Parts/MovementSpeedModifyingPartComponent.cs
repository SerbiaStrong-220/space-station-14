// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.EntityEffects.Effects.StatusEffects;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
public sealed partial class MovementSpeedModifyingPartComponent : Component
{
    [DataField]
    public bool RequiresActive = true;

    [DataField]
    public MovementSpeedModifier SpeedMod = new MovementSpeedModifier();
}
