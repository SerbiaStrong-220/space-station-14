using Content.Shared.SS220.Shuttles.UI;
using System.Linq;

namespace Content.Server.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    public override void UpdateClients()
    {
        base.UpdateClients();

        var msg = new ShuttleNavInfoUpdateMessage([.. _infos.Values]);
        RaiseNetworkEvent(msg);
    }
}
