// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.SS220.AutoLinkingTeleport;
using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Server.SS220.AutoLinkingTeleport;

public sealed partial class AutoLinkingTeleportSystem : SharedAutoLinkingTeleportSystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLinkingTeleportComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutoLinkingTeleportComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<AutoLinkingTeleportComponent> ent, ref MapInitEvent args)
    {
        TryFindNewLink(ent);
    }

    private void OnRemove(Entity<AutoLinkingTeleportComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.LinkedTeleporter is not { } linkedTeleporter)
            return;

        if (TryComp<AutoLinkingTeleportComponent>(linkedTeleporter, out var linkedTeleporterComp))
        {
            linkedTeleporterComp.LinkedTeleporter = null;
            TryFindNewLink((linkedTeleporter, linkedTeleporterComp));
        }

        ent.Comp.LinkedTeleporter = null;
        UpdateVisuals(ent);
    }

    private bool TryFindNewLink(Entity<AutoLinkingTeleportComponent> ent)
    {
        if (ent.Comp.LinkedTeleporter != null)
            return false;

        UpdateVisuals(ent);

        var query = EntityQueryEnumerator<AutoLinkingTeleportComponent>();
        while (query.MoveNext(out var candidate, out var candidateComp))
        {
            if (candidate == ent.Owner)
                continue;

            if (TerminatingOrDeleted(candidate))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.LinkWhitelist, candidate))
                continue;

            if (Transform(ent).MapID != Transform(candidate).MapID)
            {
                if (!ent.Comp.CanLinkToOtherMaps)
                    continue;

                if (!candidateComp.CanLinkToOtherMaps)
                    continue;
            }

            if (candidateComp.LinkedTeleporter != null)
                continue;

            ent.Comp.LinkedTeleporter = candidate;
            candidateComp.LinkedTeleporter = ent;
            UpdateVisuals(ent);
            UpdateVisuals((candidate, candidateComp));

            return true;
        }

        return false;
    }

    protected override bool TryTeleport(
        Entity<AutoLinkingTeleportComponent> ent,
        EntityUid target,
        EntityUid user,
        EntityCoordinates destinationCoordinates)
    {
        if (!base.TryTeleport(ent, target, user, destinationCoordinates))
            return false;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked teleporter {ToPrettyString(ent):teleport enter} and teleported {ToPrettyString(target):target} to {ToPrettyString(ent.Comp.LinkedTeleporter):teleport exit}");
        return true;
    }
}
