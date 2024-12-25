using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Tesla.Components;
using Content.Server.Lightning;
// SS220-SM-fix-begin
using Content.Shared.Damage;
using Content.Server.SS220.SuperMatterCrystal.Components;
// SS220-SM-fix-begin

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// Generates electricity from lightning bolts
/// </summary>
public sealed class TeslaCoilSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    //SS220-fix-SM-begin
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    //SS220-fix-SM-begin

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaCoilComponent, HitByLightningEvent>(OnHitByLightning);
        // SS220-SM-fix-begin
        SubscribeLocalEvent<TeslaCoilComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<TeslaCoilComponent, AnchorStateChangedEvent>(OnAnchorChange);
        // SS220-SM-fix-begin
    }

    //When struck by lightning, charge the internal battery
    private void OnHitByLightning(Entity<TeslaCoilComponent> coil, ref HitByLightningEvent args)
    {
        if (TryComp<BatteryComponent>(coil, out var batteryComponent))
        {
            _battery.SetCharge(coil, batteryComponent.CurrentCharge + coil.Comp.ChargeFromLightning);
        }
    }

    //SS220-SM-fix-begin
    private void OnAnchorChange(Entity<TeslaCoilComponent> entity, ref AnchorStateChangedEvent _)
    {
        TryFindSMNear(entity);
    }

    private void OnDamage(Entity<TeslaCoilComponent> entity, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null || !entity.Comp.NearSM)
            return;

        if (!args.DamageDelta.DamageDict.TryGetValue("Structural", out var structuralDamage))
            return;

        DamageSpecifier damage = new();
        damage.DamageDict.Add("Structural", -1 * structuralDamage * entity.Comp.StructureDamageRecoveredNearSM);

        _damageable.TryChangeDamage(entity, damage, true);
    }

    private void TryFindSMNear(Entity<TeslaCoilComponent> entity)
    {
        var smEntitiesInRange = _entityLookup.GetEntitiesInRange<SuperMatterComponent>(Transform(entity).Coordinates, entity.Comp.LookupSMRange);
        entity.Comp.NearSM = !(smEntitiesInRange.Count == 0);
    }
    //SS220-SM-fix-end
}
