// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Shuttles.UI;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Shuttles;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowNavInfoComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ShuttleNavInfo NavInfo;
}
