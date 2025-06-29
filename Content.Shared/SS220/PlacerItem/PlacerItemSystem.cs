// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Construction;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.RCD.Systems;
using Content.Shared.SS220.PlacerItem.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PlacerItem.Systems;

public sealed partial class PlacerItemSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly RCDSystem _rcdSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlacerItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlacerItemComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlacerItemComponent, PlacerItemDoAfterEvent>(OnDoAfter);

        SubscribeNetworkEvent<PlacerItemUpdateDirectionEvent>(OnUpdateDirection);
    }

    private void OnUseInHand(Entity<PlacerItemComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled || !entity.Comp.ToggleActiveOnUseInHand)
            return;

        SetActive(entity, !entity.Comp.Active);
        args.Handled = true;
    }

    private void OnAfterInteract(Entity<PlacerItemComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !entity.Comp.Active)
            return;

        var (uid, comp) = entity;
        var user = args.User;
        var location = args.ClickLocation;

        if (!location.IsValid(EntityManager))
            return;

        var gridUid = _xform.GetGrid(location);
        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var posVector = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);
        if (!IsPlacementOperationStillValid(entity, (gridUid.Value, mapGrid), posVector, args.Target, user))
            return;

        var ev = new PlacerItemDoAfterEvent(GetNetCoordinates(location), comp.ConstructionDirection, comp.SpawnProto);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, comp.DoAfter, ev, uid, args.Target, uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            comp.Active = false;
            args.Handled = true;
        };
    }

    private void OnDoAfter(Entity<PlacerItemComponent> entity, ref PlacerItemDoAfterEvent args)
    {
        if (args.Cancelled || !_net.IsServer)
            return;

        var gridUid = _xform.GetGrid(GetCoordinates(args.Location));
        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var posVector = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, GetCoordinates(args.Location));
        var mapCords = _xform.ToMapCoordinates(_mapSystem.GridTileToLocal(gridUid.Value, mapGrid, posVector));
        Spawn(args.ProtoId.Id, mapCords, rotation: args.Direction.ToAngle());

        QueueDel(entity);
    }

    private void OnUpdateDirection(PlacerItemUpdateDirectionEvent ev, EntitySessionEventArgs session)
    {
        var uid = GetEntity(ev.NetEntity);
        var user = session.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryComp<HandsComponent>(user, out var hands) ||
            uid != hands.ActiveHand?.HeldEntity)
            return;

        if (!TryComp<PlacerItemComponent>(uid, out var comp))
            return;

        comp.ConstructionDirection = ev.Direction;
        Dirty(uid, comp);
    }

    private void SetActive(Entity<PlacerItemComponent> entity, bool value)
    {
        entity.Comp.Active = value;
        Dirty(entity);
    }

    public bool IsPlacementOperationStillValid(Entity<PlacerItemComponent> entity, Entity<MapGridComponent> grid, Vector2i position, EntityUid? target, EntityUid user)
    {
        var (uid, comp) = entity;
        var unobstracted = target == null
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(grid, grid, position))
            : _interaction.InRangeUnobstructed(user, target.Value);

        if (!unobstracted)
            return false;

        if (!_prototypeManager.TryIndex<EntityPrototype>(comp.SpawnProto.Id, out var prototype))
            return false;

        prototype.TryGetComponent<TagComponent>(_factory.GetComponentName<TagComponent>(), out var tagComponent);
        var isWindow = tagComponent?.Tags != null && tagComponent.Tags.Contains("Window");
        var isCatwalk = tagComponent?.Tags != null && tagComponent.Tags.Contains("Catwalk");

        var intersectingEntities = _lookup.GetLocalEntitiesIntersecting(grid, position, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in intersectingEntities)
        {
            if (isWindow && HasComp<SharedCanBuildWindowOnTopComponent>(ent))
                continue;

            if (isCatwalk && _tag.HasTag(ent, "Catwalk"))
            {
                return false;
            }

            if (prototype.TryGetComponent<FixturesComponent>(_factory.GetComponentName<FixturesComponent>(), out var protoFixtures) &&
                TryComp<FixturesComponent>(ent, out var entFixtures) &&
                IsOurEntWillCollideWithOtherEnt(comp.ConstructionTransform, protoFixtures, ent, entFixtures))
                return false;
        }

        return true;
    }

    private bool IsOurEntWillCollideWithOtherEnt(Transform ourXform, FixturesComponent ourFixtures, EntityUid otherUid, FixturesComponent otherFixtures)
    {
        foreach (var ourFixture in ourFixtures.Fixtures.Values)
        {
            if (!ourFixture.Hard || ourFixture.CollisionLayer <= 0)
                continue;

            foreach (var otherFixture in otherFixtures.Fixtures.Values)
            {
                if (!otherFixture.Hard || otherFixture.CollisionLayer <= 0 ||
                    (ourFixture.CollisionLayer & otherFixture.CollisionLayer) == 0)
                    continue;

                var otherXformComp = Transform(otherUid);
                var otherXform = new Transform(new(), otherXformComp.LocalRotation);

                if (ourFixture.Shape.ComputeAABB(ourXform, 0).Intersects(otherFixture.Shape.ComputeAABB(otherXform, 0)))
                    return true;
            }
        }

        return false;
    }
}

[Serializable, NetSerializable]
public sealed partial class PlacerItemDoAfterEvent : DoAfterEvent
{
    public NetCoordinates Location;
    public Direction Direction;
    public EntProtoId ProtoId;

    public PlacerItemDoAfterEvent(NetCoordinates location, Direction direction, EntProtoId protoId)
    {
        Location = location;
        Direction = direction;
        ProtoId = protoId;
    }

    public override DoAfterEvent Clone() => this;
}
