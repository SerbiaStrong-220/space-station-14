using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.StepTrigger.Systems;
using Content.Server.Explosion.EntitySystems; // SS220-vehicles-go-boom-in-lava
using Content.Shared.SS220.Vehicle.Components; // SS220-vehicles-go-boom-in-lava
using Content.Shared.Damage; // SS220-vehicles-go-boom-in-lava
using Content.Shared.Damage.Systems; // SS220-vehicles-go-boom-in-lava

namespace Content.Server.Tiles;

public sealed class LavaSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private ExplosionSystem _explosionSystem = default!; // SS220-vehicles-go-boom-in-lava
    [Dependency] private readonly DamageableSystem _damageable = default!; // SS220-vehicles-go-boom-in-lava

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LavaComponent, StepTriggeredOffEvent>(OnLavaStepTriggered);
        SubscribeLocalEvent<LavaComponent, StepTriggerAttemptEvent>(OnLavaStepTriggerAttempt);
    }

    private void OnLavaStepTriggerAttempt(EntityUid uid, LavaComponent component, ref StepTriggerAttemptEvent args)
    {
        // SS220-vehicles-go-boom-in-lava-start
        if (HasComp<VehicleComponent>(args.Tripper))
        {
            _explosionSystem.QueueExplosion(args.Tripper, "Default", 3, 25f, 10, canCreateVacuum:false); // Not deadly to rider but still painful
            if (TryComp<DamageableComponent>(args.Tripper, out var damageablecomp))
            {
                var damageToVehicle = 600;
                _damageable.SetAllDamage(args.Tripper, damageablecomp, damageToVehicle); // Hardcoded number but its enough to destroy any vehicle rn
            }
            return;
        }
        // SS220-vehicles-go-boom-in-lava-end

        if (!HasComp<FlammableComponent>(args.Tripper))
            return;

        args.Continue = true;
    }

    private void OnLavaStepTriggered(EntityUid uid, LavaComponent component, ref StepTriggeredOffEvent args)
    {
        var otherUid = args.Tripper;

        if (TryComp<FlammableComponent>(otherUid, out var flammable))
        {
            // Apply the fury of a thousand suns
            var multiplier = flammable.FireStacks == 0f ? 5f : 1f;
            _flammable.AdjustFireStacks(otherUid, component.FireStacks * multiplier, flammable);
            _flammable.Ignite(otherUid, uid, flammable);
        }
    }
}
