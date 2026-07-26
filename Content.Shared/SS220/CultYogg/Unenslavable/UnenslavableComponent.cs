// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.Unenslavable;

/// <summary>
/// Used to markup mindshielded targets for cultists
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnenslavableComponent : Component
{
    [ViewVariables]
    public ProtoId<FactionIconPrototype> StatusIcon = "CultYoggUnenslavableIcon";
}
