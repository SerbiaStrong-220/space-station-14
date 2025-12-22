// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.SS220.MindExtension;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MindExtensionComponent : Component
{
    public NetUserId Player;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<NetEntity, TrailPointMetaData> Trail = [];

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan? RespawnTimer = default!;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan RespawnTime = TimeSpan.FromMinutes(20);

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RespawnAvailable = false;
}
