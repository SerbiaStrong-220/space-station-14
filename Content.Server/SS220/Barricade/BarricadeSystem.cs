// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Projectiles;
using Content.Shared.SS220.Barricade;
using Microsoft.Extensions.DependencyModel;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.SS220.Barricade;

public sealed partial class BarricadeSystem : SharedBarricadeSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override bool HitscanTryPassBarricade(Entity<BarricadeComponent> entity, EntityUid source, TransformComponent? sourceXform = null)
    {
        if (!Resolve(source, ref sourceXform))
            return false;

        var hitChance = CalculateHitChance(entity, sourceXform.GridUid, sourceXform.LocalPosition, _transform.GetWorldPosition(source));
        var isHit = _random.Prob(hitChance);

        return !isHit;
    }

    protected override bool ProjectileTryPassBarricade(Entity<BarricadeComponent> entity, Entity<ProjectileComponent> projEnt)
    {
        var (uid, comp) = entity;
        var (projUid, projComp) = projEnt;

        var passBarricade = EnsureComp<PassBarricadeComponent>(projUid);
        if (passBarricade.CollideBarricades.TryGetValue(uid, out var isPass))
            return isPass;

        var hitChance = CalculateHitChance(entity, projComp.ShootGridUid, projComp.ShootGridPos, projComp.ShootWorldPos);
        var isHit = _random.Prob(hitChance);

        passBarricade.CollideBarricades.Add(uid, !isHit);
        Dirty(projUid, passBarricade);

        return !isHit;
    }

    private float CalculateHitChance(Entity<BarricadeComponent> entity, EntityUid? gridUid = null, Vector2? gridPos = null, Vector2? worldPos = null)
    {
        var (uid, comp) = entity;
        var xform = Transform(entity);

        float distance;
        if (gridUid != null && gridPos != null && xform.ParentUid == gridUid)
        {
            var posDiff = xform.LocalPosition - gridPos;
            distance = posDiff.Value.Length();
        }
        else if (worldPos != null)
        {
            var posDiff = _transform.GetWorldPosition(uid) - worldPos;
            distance = posDiff.Value.Length();
        }
        else
        {
            distance = comp.MaxDistance;
        }

        var distanceDiff = comp.MaxDistance - comp.MinDistance;
        var chanceDiff = comp.MaxHitChance - comp.MinHitChance;

        /// How much the <see cref="BarricadeComponent.MinHitChances"/> will increase.
        var increaseChance = Math.Clamp(distance - comp.MinDistance, 0, distanceDiff) / distanceDiff * chanceDiff;

        var hitChance = Math.Clamp(comp.MinHitChance + increaseChance, comp.MinHitChance, comp.MaxHitChance);

        return hitChance;
    }
}
