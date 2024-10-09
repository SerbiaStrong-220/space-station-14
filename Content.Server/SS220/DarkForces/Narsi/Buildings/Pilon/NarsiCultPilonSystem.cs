using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Polymorth;
using Content.Server.SS220.GameRules.Cult.Narsi.Buildings.Pilon;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Pilon;

public sealed class NarsiCultPilonSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var pilonQuery = EntityQueryEnumerator<NarsiCultPilonComponent>();

        while (pilonQuery.MoveNext(out var uid, out var pilon))
        {
            if (pilon.LastTick + TimeSpan.FromSeconds(7) > curTime)
                continue;

            pilon.LastTick = curTime;

            var pilonCoords = Transform(uid).Coordinates;
            var pilonsNear = _entityLookupSystem.GetEntitiesInRange<NarsiCultPilonComponent>(pilonCoords, 10f);
            if (pilonsNear.Count > 2)
                continue;

            var cultistsNear = _entityLookupSystem.GetEntitiesInRange<NarsiCultistComponent>(pilonCoords, 6.0f);
            if (cultistsNear.Any())
                RegenCultists(cultistsNear, pilon);

            var cultistsPolymorphNear = _entityLookupSystem.GetEntitiesInRange<NarsiPolymorphComponent>(pilonCoords, 6.0f);
            if (cultistsPolymorphNear.Any())
                RegenPolymorph(cultistsPolymorphNear, pilon);
        }
    }

    private void RegenPolymorph(HashSet<Entity<NarsiPolymorphComponent>> cultists, NarsiCultPilonComponent pilonComponent)
    {
        foreach (var cultist in cultists)
        {
            var uid = cultist.Owner;
            RegenEntity(uid, pilonComponent);
        }
    }

    private void RegenCultists(HashSet<Entity<NarsiCultistComponent>> cultists, NarsiCultPilonComponent pilonComponent)
    {
        foreach (var cultist in cultists)
        {
            var uid = cultist.Owner;
            RegenEntity(uid, pilonComponent);
        }
    }

    private void RegenEntity(EntityUid uid, NarsiCultPilonComponent pilonComponent)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) || _mobStateSystem.IsDead(uid))
            return;

        _damageable.TryChangeDamage(uid, pilonComponent.HealingDamage, true, false, damageable);
    }
}
