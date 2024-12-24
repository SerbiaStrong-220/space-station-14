using Content.Shared.Projectiles;
using Content.Shared.SS220.Barricade;
using Microsoft.Extensions.DependencyModel;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.SS220.Barricade;

public sealed partial class BarricadeSystem : SharedBarricadeSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void CalculateChance(Entity<BarricadeComponent> entity, Entity<ProjectileComponent> projEnt)
    {
        base.CalculateChance(entity, projEnt);

        var (uid, comp) = entity;
        var (projUid, projComp) = projEnt;

        var passBarricade = EnsureComp<PassBarricadeComponent>(projUid);
        if (passBarricade.CollideBarricades.ContainsKey(uid))
            return;

        float distance;
        if (projComp.ShootGtidUid != null && projComp.ShootGridPos != null && projComp.ShootWorldPos != null)
        {
            var xform = Transform(entity);
            var posdiff = xform.ParentUid == projComp.ShootGtidUid
                ? xform.LocalPosition - projComp.ShootGridPos
                : _transform.GetWorldPosition(uid) - projComp.ShootWorldPos;

            distance = posdiff.Value.Length();
        }
        else
            distance = comp.MaxDistance;

        var distanceDiff = comp.MaxDistance - comp.MinDistance;
        var changeDiff = comp.MaxHitChance - comp.MinHitChance;

        /// How much the <see cref="BarricadeComponent.MinHitChances"/> will increase.
        var increaseChance = Math.Clamp(distance - comp.MinDistance, 0, distanceDiff) / distanceDiff * changeDiff;

        var hitChance = Math.Clamp(comp.MinHitChance + increaseChance, comp.MinHitChance, comp.MaxHitChance);
        var isHit = _random.Prob(hitChance);

        passBarricade.CollideBarricades.Add(uid, isHit);
        Dirty(projUid, passBarricade);

        return;
    }
}
