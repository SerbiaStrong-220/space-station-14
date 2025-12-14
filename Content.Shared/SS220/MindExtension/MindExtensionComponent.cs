// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Network;

namespace Content.Shared.SS220.MindExtension;

[RegisterComponent]
public sealed partial class MindExtensionComponent : Component
{
    public NetUserId Player;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, TrailPointMetaData> Trail = [];

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? RespawnTimer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RespawnTime = TimeSpan.FromMinutes(20);

    [ViewVariables]
    public bool RespawnAvailable = false;
}
