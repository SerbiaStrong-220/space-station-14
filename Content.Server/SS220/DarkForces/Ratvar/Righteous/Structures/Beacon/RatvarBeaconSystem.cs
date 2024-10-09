using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Structures;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Beacon;

public sealed class RatvarBeaconSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RatvarProgressSystem _ratvarProgress = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly TimeSpan _healTime = TimeSpan.FromSeconds(7);
    private readonly TimeSpan _powerTime = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarBeaconComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RatvarBeaconComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentInit(EntityUid uid, RatvarBeaconComponent component, ComponentInit args)
    {
        component.LastHealTick = _timing.CurTime;
        component.LastPowerTick = _timing.CurTime;

        var transform = Transform(uid);
        var beaconsNeably = _entityLookup.GetEntitiesInRange<RatvarBeaconComponent>(transform.Coordinates, 10f);
        if (beaconsNeably.Count > 1)
        {
            foreach (var beacon in beaconsNeably)
            {
                beacon.Comp.Enabled = false;
                _sharedAppearance.SetData(beacon, BeaconVisuals.State, false);
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, RatvarBeaconComponent component, ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<RatvarBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var beacon, out var beaconComp, out var transform))
        {
            var beaconsNeably = _entityLookup.GetEntitiesInRange<RatvarBeaconComponent>(transform.Coordinates, 10f);
            if (beaconsNeably.Count > 1)
                continue;

            beaconComp.Enabled = true;
            _sharedAppearance.SetData(beacon, BeaconVisuals.State, true);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<RatvarBeaconComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Enabled)
                continue;

            if (component.LastHealTick <= curTime)
            {
                HealRighteouses(uid, component);
                component.LastHealTick = curTime + _healTime;
            }

            if (component.LastPowerTick <= curTime)
            {
                IncreasePower(component);
                component.LastPowerTick = curTime + _powerTime;
            }
        }
    }

    private void IncreasePower(RatvarBeaconComponent component)
    {
        _ratvarProgress.TryRequestChangePower(component.PowerPerTick);
    }

    private void HealRighteouses(EntityUid beacon, RatvarBeaconComponent component)
    {
        var transform = Transform(beacon);
        var entities = _entityLookup.GetEntitiesInRange<RatvarRighteousComponent>(transform.Coordinates, 10f);

        foreach (var entity in entities)
        {
            if (!TryComp<DamageableComponent>(entity, out var damageable) || _mobState.IsDead(entity))
                continue;

            _damageable.TryChangeDamage(entity, component.HealingDamage, true, false, damageable);
        }
    }
}
