// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.FieldShield;

public sealed class FieldShieldProviderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const int FieldShieldPushPriority = 2;

    private static readonly LocId FieldShieldOn = "field-shield-provider-on";
    private static readonly LocId FieldShieldOff = "field-shield-provider-off";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldShieldComponent, MapInitEvent>(OnFieldShieldMapInit);
        SubscribeLocalEvent<FieldShieldComponent, ComponentRemove>(OnFieldShieldRemove);

        SubscribeLocalEvent<FieldShieldComponent, ExaminedEvent>(OnFieldShieldExamined);
        SubscribeLocalEvent<FieldShieldProviderComponent, ExaminedEvent>(OnFieldShieldProviderExamined);

        SubscribeLocalEvent<FieldShieldProviderComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<FieldShieldProviderComponent, BeingUnequippedAttemptEvent>(OnUneqippingAttempt);

        SubscribeLocalEvent<FieldShieldProviderComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<FieldShieldProviderComponent, ItemToggledEvent>(OnToggled);

        SubscribeLocalEvent<FieldShieldProviderComponent, GotEquippedEvent>(OnProviderEquipped);
        SubscribeLocalEvent<FieldShieldProviderComponent, GotUnequippedEvent>(OnProviderUnequipped);

        SubscribeLocalEvent<FieldShieldComponent, BeforeDamageChangedEvent>(OnFieldShieldBeforeDamage);
        SubscribeLocalEvent<FieldShieldComponent, DamageModifyEvent>(OnShieldDamageModify);

        SubscribeLocalEvent<FieldShieldProviderComponent, EmpPulseEvent>(OnFieldShieldProviderEmpPulse);
        SubscribeLocalEvent<FieldShieldComponent, EmpPulseEvent>(OnFieldShieldEmpPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var fieldShields = EntityQueryEnumerator<FieldShieldComponent, UpdateQueuedFieldShieldComponent>();

        while (fieldShields.MoveNext(out var uid, out var comp, out var updateComp))
        {
            if (_gameTiming.CurTime < comp.RechargeEndTime)
                continue;

            RemCompDeferred(uid, updateComp);
            comp.ShieldCharge = comp.ShieldData.ShieldMaxCharge;

            DirtyField(uid, comp, nameof(FieldShieldComponent.ShieldCharge));
        }
    }

    private void OnFieldShieldMapInit(Entity<FieldShieldComponent> entity, ref MapInitEvent _)
    {
        entity.Comp.RechargeEndTime = _gameTiming.CurTime + entity.Comp.RechargeShieldData.RechargeTime;

        EnsureComp<UpdateQueuedFieldShieldComponent>(entity);
        DirtyField(entity!, nameof(FieldShieldComponent.RechargeEndTime));
    }

    private void OnFieldShieldRemove(Entity<FieldShieldComponent> entity, ref ComponentRemove _)
    {
        RemCompDeferred<UpdateQueuedFieldShieldComponent>(entity);
    }

    private void OnFieldShieldExamined(Entity<FieldShieldComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Owner == args.Examiner)
        {
            args.PushMarkup(Loc.GetString("field-shield-self-examine", ("Charges", entity.Comp.ShieldCharge), ("MaxCharge", entity.Comp.ShieldData.ShieldMaxCharge)), FieldShieldPushPriority);
        }
        else if (entity.Comp.ShieldCharge > 0)
        {
            args.PushMarkup(Loc.GetString("field-shield-other-examine"), FieldShieldPushPriority);
        }
    }

    private void OnFieldShieldProviderExamined(Entity<FieldShieldProviderComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("field-shield-provider-examine", ("ChargeTime", entity.Comp.RechargeShieldData.RechargeTime), ("MaxCharge", entity.Comp.ShieldData.ShieldMaxCharge)));
    }

    private void OnBeingEquippedAttempt(Entity<FieldShieldProviderComponent> entity, ref BeingEquippedAttemptEvent args)
    {
        if (!HasComp<FieldShieldComponent>(args.User))
            return;

        args.Cancel();

        _popup.PopupClient(Loc.GetString("field-shield-provider-cant-equip-when-you-already-have-one"), args.User);
    }

    private void OnUneqippingAttempt(Entity<FieldShieldProviderComponent> entity, ref BeingUnequippedAttemptEvent args)
    {
        if (_gameTiming.CurTime > entity.Comp.UnLockAfterEmpTime)
            return;

        args.Cancel();
        args.Reason = "field-shield-provider-cant-unequip-when-emped";
    }

    private void OnActivateAttempt(Entity<FieldShieldProviderComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        args.Cancelled = !ent.Comp.Equipped;
    }


    private void OnToggled(Entity<FieldShieldProviderComponent> ent, ref ItemToggledEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.User == null)
            return;

        var user = args.User.Value;

        var message = Loc.GetString(args.Activated ? FieldShieldOn : FieldShieldOff);
        _popup.PopupClient(message, user, user);

        if (args.Activated)
        {
            var shieldComp = EnsureComp<FieldShieldComponent>(user);
            shieldComp.ShieldData = ent.Comp.ShieldData;
            shieldComp.RechargeShieldData = ent.Comp.RechargeShieldData;
            shieldComp.LightData = ent.Comp.LightData;

            shieldComp.RechargeEndTime = _gameTiming.CurTime + ent.Comp.RechargeShieldData.RechargeTime;
            Dirty(user, shieldComp);
        }
        else
        {
            RemCompDeferred<FieldShieldComponent>(user);
        }
    }

    private void OnProviderEquipped(Entity<FieldShieldProviderComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.Equipped = true;
    }

    private void OnProviderUnequipped(Entity<FieldShieldProviderComponent> entity, ref GotUnequippedEvent args)
    {
        RemCompDeferred<FieldShieldComponent>(args.EquipTarget);
        entity.Comp.Equipped = false;
    }

    private void OnFieldShieldBeforeDamage(Entity<FieldShieldComponent> entity, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Damage.GetTotal() > entity.Comp.ShieldData.MaxDamageConsumable
            || args.Damage.GetTotal() < entity.Comp.ShieldData.DamageThreshold)
            return;

        UpdateShieldTimer(entity);

        if (entity.Comp.ShieldCharge <= 0)
            return;

        DecreaseShieldCharges(entity);
        args.Cancelled = true;
    }

    private void OnShieldDamageModify(Entity<FieldShieldComponent> entity, ref DamageModifyEvent args)
    {
        if (args.OriginalDamage.GetTotal() < entity.Comp.ShieldData.DamageThreshold)
            return;

        UpdateShieldTimer(entity);

        if (entity.Comp.ShieldCharge <= 0)
            return;

        DecreaseShieldCharges(entity);
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, entity.Comp.ShieldData.Modifiers);
    }

    private void DecreaseShieldCharges(Entity<FieldShieldComponent> entity)
    {
        entity.Comp.ShieldCharge--;
        _audio.PlayPredicted(entity.Comp.ShieldData.ShieldBlockSound, entity, entity);

        DirtyField(entity!, nameof(FieldShieldComponent.ShieldCharge));
    }

    private void UpdateShieldTimer(Entity<FieldShieldComponent> entity)
    {
        var newRechargeTime = _gameTiming.CurTime + entity.Comp.RechargeShieldData.RechargeTime;
        entity.Comp.RechargeEndTime = newRechargeTime > entity.Comp.RechargeEndTime
                                        ? newRechargeTime
                                        : entity.Comp.RechargeEndTime;

        // ensure comp breaks prediction reset
        if (_gameTiming.IsFirstTimePredicted)
            EnsureComp<UpdateQueuedFieldShieldComponent>(entity);

        DirtyField(entity!, nameof(FieldShieldComponent.RechargeEndTime));
    }

    private void OnFieldShieldProviderEmpPulse(Entity<FieldShieldProviderComponent> entity, ref EmpPulseEvent args)
    {
        if (!entity.Comp.LockOnEmp)
            return;

        args.Affected = true;
        entity.Comp.UnLockAfterEmpTime = _gameTiming.CurTime + entity.Comp.RechargeShieldData.RechargeTime * (entity.Comp.RechargeShieldData.EmpRechargeMultiplier - 1);
        DirtyField(entity!, nameof(FieldShieldProviderComponent.UnLockAfterEmpTime));
    }

    private void OnFieldShieldEmpPulse(Entity<FieldShieldComponent> entity, ref EmpPulseEvent args)
    {
        args.Affected = true;
        // cause of naming it goes -1f.
        entity.Comp.RechargeEndTime = _gameTiming.CurTime + entity.Comp.RechargeShieldData.RechargeTime * entity.Comp.RechargeShieldData.EmpRechargeMultiplier;
        entity.Comp.ShieldCharge = 0;
        Dirty(entity);
    }
}
