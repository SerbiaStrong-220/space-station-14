using Robust.Shared.Network;

namespace Content.Shared.SS220.MindExtension;

[RegisterComponent]
public sealed partial class MindExtensionComponent : Component
{
    public NetUserId PlayerSession;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, TrailPointMetaData> Trail = [];

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? RespawnTimer = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RespawnTime = TimeSpan.FromMinutes(20);


    public bool RespawnAvaible = false;
}
