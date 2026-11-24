// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Roles.Components;
using Content.Shared.SS220.AntagRemoval;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.AntagRemoval;

public sealed partial class RemoveAntagSystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private void InitializeUi()
    {
        SubscribeNetworkEvent<RequestRemovableRolesEvent>(OnRequestRoles);
        SubscribeNetworkEvent<RemoveAntagRoleEvent>(OnRemoveRole);
    }

    private void OnRequestRoles(RequestRemovableRolesEvent ev, EntitySessionEventArgs args)
    {
        SendRoleOptions(args.SenderSession, ev.RequestId, ev.Target);
    }

    private void OnRemoveRole(RemoveAntagRoleEvent ev, EntitySessionEventArgs args)
    {
        var administrator = args.SenderSession;
        if (!_adminManager.HasAdminFlag(administrator, AdminFlags.Admin))
        {
            SendRoleOptions(administrator, ev.RequestId, ev.Target);
            return;
        }

        if (!_playerManager.TryGetSessionById(ev.Target, out var target))
        {
            SendRoleOptions(administrator, ev.RequestId, ev.Target);
            return;
        }

        var removed = TryRemove(administrator, target, ev.Kind, out var result, ev.Body, ev.Mind);
        SendRoleOptions(administrator, ev.RequestId, ev.Target, removed ? string.Empty : result);
    }

    private void SendRoleOptions(ICommonSession administrator, Guid requestId, NetUserId targetId, string message = "")
    {
        var roles = new Dictionary<string, string>();
        var unsupportedRoles = new List<string>();
        NetEntity? bodyNet = null;
        NetEntity? mindNet = null;

        if (!_adminManager.HasAdminFlag(administrator, AdminFlags.Admin))
        {
            message = Loc.GetString("ss220-removeantag-ui-no-permission");
        }
        else if (!_playerManager.TryGetSessionById(targetId, out var target))
        {
            message = Loc.GetString("ss220-removeantag-error-not-connected");
        }
        else if (!TryResolvePlayer(target, out var body, out var mindId, out var mind, out var reason))
        {
            message = reason;
        }
        else
        {
            bodyNet = GetNetEntity(body);
            mindNet = GetNetEntity(mindId);

            foreach (var (kind, roleId) in Supported)
            {
                if (kind != "cultist" && HasRolePrototype(mind, roleId))
                    roles.Add(kind, RoleName(roleId));
            }

            foreach (var roleUid in mind.MindRoleContainer.ContainedEntities)
            {
                if (!TryComp<MindRoleComponent>(roleUid, out var role) || !(role.Antag || role.ExclusiveAntag) ||
                    MetaData(roleUid).EntityPrototype is not { } prototype || Supported.ContainsValue(prototype.ID))
                    continue;

                var name = role.AntagPrototype is { } antagId && _prototypes.TryIndex(antagId, out var antag)
                    ? Loc.GetString(antag.Name)
                    : Name(roleUid);
                unsupportedRoles.Add(name);
            }
        }

        RaiseNetworkEvent(new RemovableRolesEvent(requestId, targetId, bodyNet, mindNet, roles, message, unsupportedRoles), administrator.Channel);
    }
}
