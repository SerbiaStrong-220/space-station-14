// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.LyingDown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LyingDownComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ActionUid;

    [ViewVariables, AutoNetworkedField]
    public bool IsLying = false;
}
