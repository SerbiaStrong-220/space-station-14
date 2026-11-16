using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.SS220.TeleportAFKtoCryoSystem;

[Serializable, NetSerializable]
public sealed partial class TeleportToCryoFinished : SimpleDoAfterEvent
{
    public NetEntity PortalId { get; private set; }

    public TeleportToCryoFinished(NetEntity portalId)
    {
        PortalId = portalId;
    }
}
