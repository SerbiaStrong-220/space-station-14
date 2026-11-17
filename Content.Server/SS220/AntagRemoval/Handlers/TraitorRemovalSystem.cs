// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Roles.RoleCodeword;
using Content.Shared.Store.Components;

namespace Content.Server.SS220.AntagRemoval.Handlers;

public sealed class TraitorRemovalSystem : EntitySystem, IAntagRemovalHandler
{
    [Dependency] private TraitorRuleSystem _traitor = default!;
    [Dependency] private SharedRoleCodewordSystem _codewords = default!;
    [Dependency] private SharedSubdermalImplantSystem _implants = default!;

    public bool RemovesMindRole => false;

    public bool CanRemove(EntityUid body, EntityUid mindId, out string reason)
    {
        reason = string.Empty;
        return true;
    }

    public bool TryRemove(EntityUid body, EntityUid mindId,
        AntagRemovalGrantComponent provenance, AntagGrantRecord grant, out string reason)
    {
        RevokeUplinks(body, grant);
        if (TryComp<RoleCodewordComponent>(mindId, out var codewords))
            _codewords.SetRoleCodewords((mindId, codewords), "traitor", new List<string>(), default);
        _traitor.RemoveTraitorFromRoundAccounting(mindId);
        reason = string.Empty;
        return true;
    }

    private void RevokeUplinks(EntityUid body, AntagGrantRecord grant)
    {
        var stores = grant.UplinkStores.Where(uid => !TerminatingOrDeleted(uid)).ToHashSet();

        if (TryComp<ImplantedComponent>(body, out var implanted))
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities.ToArray())
                if (grant.UplinkImplants.Contains(implant))
                    _implants.ForceRemove((body, implanted), implant);
        }

        foreach (var implant in grant.UplinkImplants)
            if (!TerminatingOrDeleted(implant))
                QueueDel(implant);

        var query = EntityQueryEnumerator<RemoteStoreComponent>();
        while (query.MoveNext(out var uid, out var remote))
            if (remote.Store is { } store && stores.Contains(store))
            {
                remote.Store = null;
                Dirty(uid, remote);
            }

        foreach (var store in stores)
            QueueDel(store);
    }
}
