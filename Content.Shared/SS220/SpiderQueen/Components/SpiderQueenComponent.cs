// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpiderQueenComponent : Component
{
    /// <summary>
    /// List of actions
    /// </summary>
    [DataField]
    public List<EntProtoId>? Actions;
}
