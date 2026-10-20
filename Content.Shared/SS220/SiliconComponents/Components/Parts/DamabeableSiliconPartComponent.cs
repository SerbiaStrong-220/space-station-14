// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class DamabeableSiliconPartComponent : Component //Yeah-yeah the naming is pretty messed up
{
    [DataField]
    public FixedPoint2 MaxDamageToRemainFunctional = 35;

    [DataField]
    public FixedPoint2 MinDamageToMalfunction = 20;

    [AutoNetworkedField]
    public FixedPoint2 CurrentDamageEfficiencyModifier = 1;
}
