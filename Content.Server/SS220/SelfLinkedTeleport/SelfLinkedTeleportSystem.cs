// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.SelfLinkedTeleport;
using Content.Shared.Whitelist;

namespace Content.Server.SS220.SelfLinkedTeleport;

/// <summary>
/// </summary>
public sealed class SelfLinkedTeleportSystem : SharedSelfLinkedTeleportSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<SelfLinkedTeleportComponent> ent, ref MapInitEvent args)//not sure about an event type
    {
        TryFindNewLink(ent);
    }

    private void OnRemove(Entity<SelfLinkedTeleportComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.LinkedEntity == null)
            return;

        if (TryComp<SelfLinkedTeleportComponent>(ent, out var linkedTeleporterComp))
        {
            linkedTeleporterComp.LinkedEntity = null;
        }

        ent.Comp.LinkedEntity = null;
    }

    public void TryFindNewLink(Entity<SelfLinkedTeleportComponent> ent)
    {
        if (ent.Comp.LinkedEntity != null)
            return;

        var locations = EntityQueryEnumerator<SelfLinkedTeleportComponent>();
        while (locations.MoveNext(out var uid, out var teleport))
        {
            if (uid == ent.Owner)//shouldn't be linked to itself
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.WhitelistLinked, uid))
                continue;

            if (teleport.LinkedEntity != null)
                continue;

            ent.Comp.LinkedEntity = uid;
            teleport.LinkedEntity = ent;

            return;
        }
    }

    public void DeleteLink(Entity<SelfLinkedTeleportComponent> ent)
    {
        if (ent.Comp.LinkedEntity == null)
        {
            Log.Error($"SelfLinkedTeleport {ent} tried to remove a link that doesn't exist");
            return;
        }

        var linkedEnt = ent.Comp.LinkedEntity.Value;

        if (TryComp<SelfLinkedTeleportComponent>(linkedEnt, out var linkedTeleporterComp))//looks wierd
        {
            linkedTeleporterComp.LinkedEntity = null;
            TryFindNewLink((linkedEnt, linkedTeleporterComp));
        }

        ent.Comp.LinkedEntity = null;

        return;
    }

    protected override void WarpTo(Entity<SelfLinkedTeleportComponent> ent, EntityUid target, EntityUid user)
    {
        if (ent.Comp.LinkedEntity == null)//we shouldn't interact  at all if we are  here
            return;

        if (TryComp<PullableComponent>(target, out var pullingSystemComp))
            _pulling.TryStopPull(target, pullingSystemComp);

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked telepoter {ToPrettyString(ent):teleport enter} and tried teleport {ToPrettyString(target):target} to {ToPrettyString(ent.Comp.LinkedEntity.Value):teleport exit}");

        var xform = Transform(target);
        _transformSystem.SetCoordinates(target, xform, Transform(ent.Comp.LinkedEntity.Value).Coordinates);
    }
}
