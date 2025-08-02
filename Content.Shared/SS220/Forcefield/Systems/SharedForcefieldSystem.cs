using Content.Shared.SS220.Forcefield.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.SS220.Forcefield.Systems;

public abstract class SharedForcefieldSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcefieldComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ForcefieldComponent, HitscanAttempt>(OnHitscanAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PassForcefieldsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var toRemove = new List<NetEntity>();
            var ourAABB = _physics.GetWorldAABB(uid);

            foreach (var netEnt in comp.PassedForcefields)
            {
                var forcefieldUid = GetEntity(netEnt);
                if (!HasComp<ForcefieldComponent>(forcefieldUid))
                {
                    toRemove.Add(netEnt);
                    continue;
                }

                var forcefieldAABB = _physics.GetWorldAABB(forcefieldUid);
                if (!ourAABB.Intersects(forcefieldAABB))
                    toRemove.Add(netEnt);
            }

            foreach (var value in toRemove)
                comp.PassedForcefields.Remove(value);

            if (comp.PassedForcefields.Count <= 0)
                RemComp<PassForcefieldsComponent>(uid);
            else
                Dirty(uid, comp);
        }
    }

    private void OnPreventCollide(Entity<ForcefieldComponent> entity, ref PreventCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        var netEnt = GetNetEntity(entity);

        if (TryComp<PassForcefieldsComponent>(otherUid, out var passComp) &&
            passComp.PassedForcefields.Contains(netEnt))
        {
            args.Cancelled = true;
            return;
        }

        if (!ShouldCollide(entity, otherUid))
        {
            args.Cancelled = true;

            passComp ??= AddComp<PassForcefieldsComponent>(otherUid);
            passComp.PassedForcefields.Add(netEnt);
            Dirty(otherUid, passComp);
        }
    }

    private void OnHitscanAttempt(Entity<ForcefieldComponent> entity, ref HitscanAttempt args)
    {
        if (!ShouldCollide(entity, args.User))
            args.Cancelled = true;
    }

    public bool ShouldCollide(Entity<ForcefieldComponent> entity, EntityUid uid)
    {
        if (IsInside(entity, uid))
        {
            if ((entity.Comp.Params.CollisionOption & ForcefieldCollisionOptions.InsideGoing) == 0)
                return false;
        }
        else
        {
            if ((entity.Comp.Params.CollisionOption & ForcefieldCollisionOptions.OutsideGoing) == 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Is the entity inside the shape
    /// </summary>
    public bool IsInside(Entity<ForcefieldComponent> entity, EntityUid uid)
    {
        return IsInside(entity, _transform.GetMapCoordinates(uid));
    }

    /// <summary>
    /// Is the world point inside the shape
    /// </summary>
    public bool IsInside(Entity<ForcefieldComponent> entity, MapCoordinates worldPos)
    {
        var forcefieldMap = _transform.GetMapCoordinates(entity).MapId;
        if (forcefieldMap != worldPos.MapId)
            return false;

        var localCoords = _transform.ToCoordinates(entity.Owner, worldPos);
        return entity.Comp.Params.Shape.IsInside(localCoords.Position);
    }
}
