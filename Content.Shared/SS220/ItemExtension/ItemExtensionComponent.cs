// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ItemExtension;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemExtensionComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 MinimalStrengthToPickUp = 1;

    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 StrengthRequirementToBeUsed = 1;
}
