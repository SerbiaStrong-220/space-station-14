// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
public sealed partial class StrengthModifyingPartComponent : Component
{
    [DataField]
    public FixedPoint2 StrengthValue = 1.3;
}
