// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.AntagRemoval;
using Robust.Shared.Network;

namespace Content.Client.SS220.AntagRemoval;

public sealed class AntagRemovalSystem : EntitySystem
{
    public event Action<RemovableRolesEvent>? RolesReceived;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RemovableRolesEvent>(OnRolesReceived);
    }

    public void RequestRoles(Guid requestId, NetUserId target)
    {
        RaiseNetworkEvent(new RequestRemovableRolesEvent(requestId, target));
    }

    public void RequestRemoval(Guid requestId, NetUserId target, NetEntity body, NetEntity mind, string kind)
    {
        RaiseNetworkEvent(new RemoveAntagRoleEvent(requestId, target, body, mind, kind));
    }

    private void OnRolesReceived(RemovableRolesEvent ev)
    {
        RolesReceived?.Invoke(ev);
    }
}
