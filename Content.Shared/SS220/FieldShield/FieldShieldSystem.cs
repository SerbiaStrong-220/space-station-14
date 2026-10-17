// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.FieldShield;

public sealed partial class FieldShieldProviderSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;

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

        SubscribeLocalEvent<FieldShieldProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        SubscribeLocalEvent<FieldShieldComponent, ProjectileBlockAttemptEvent>(OnShieldUserCollide);
        SubscribeLocalEvent<FieldShieldComponent, HitscanBlockAttemptEvent>(OnShieldUserHitscan);
        SubscribeLocalEvent<FieldShieldComponent, AttackedEvent>(OnShieldUserMeleeHit);
        SubscribeLocalEvent<FieldShieldComponent, ThrowableProjectileBlockAttemptEvent>(OnBlockThrownProjectile);

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

        ent.Comp.Wearer = args.User;

        if (args.User is not { Valid: true } user)
            return;

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
        entity.Comp.Wearer = null;

        RemCompDeferred<FieldShieldComponent>(args.EquipTarget);
        entity.Comp.Equipped = false;
    }

    private void OnGetAltVerbs(Entity<FieldShieldProviderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (ent.Comp.Wearer != args.User)
            return;

        foreach (var (id, mode) in ent.Comp.Modes)
        {
            if (id == ent.Comp.Mode)
                continue;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("field-shield-set-mode", ("mode", Loc.GetString(id))),
                Act = () => SetMode(ent, id),
                Priority = 2
            });
        }
    }

    private void OnShieldUserCollide(Entity<FieldShieldComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        if (!TryComp<ProjectileComponent>(args.ProjUid, out var projComp))
            return;//if a projectile has no projectile component absence of logic handling it here is the least problematic part of the problem

        if (TryBlockDamage(ent, projComp.Shooter, ref args.Damage))
            args.Cancelled = true;
    }

    private void OnBlockThrownProjectile(Entity<FieldShieldComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        if (args.Damage != null && TryBlockDamage(ent, null, ref args.Damage))
            args.Cancelled = true;
    }

    private void OnShieldUserHitscan(Entity<FieldShieldComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        if (args.Damage != null && TryBlockDamage(ent, args.Shooter, ref args.Damage))
            args.Cancelled = true;
    }

    private void OnShieldUserMeleeHit(Entity<FieldShieldComponent> ent, ref AttackedEvent args)
    {
        if (ent.Comp.ShieldCharge > 0)
        {
            args.ModifiersList.Add(ent.Comp.ShieldData.Modifiers);
            DecreaseShieldCharges(ent);
        }
    }

    private bool TryBlockDamage(Entity<FieldShieldComponent> entity, EntityUid? attacker, ref DamageSpecifier damage)
    {
        if (damage.GetTotal() < entity.Comp.ShieldData.DamageThreshold)
            return false;

        UpdateShieldTimer(entity);

        if (entity.Comp.ShieldCharge <= 0)
            return false;

        DecreaseShieldCharges(entity);
        damage = DamageSpecifier.ApplyModifierSet(damage, entity.Comp.ShieldData.Modifiers);
        _damageable.TryChangeDamage(entity.Owner, damage, out var damageResult, origin: attacker);
        return true;
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

    public void SetMode(Entity<FieldShieldProviderComponent> ent, string mode)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (ent.Comp.Wearer is not { Valid: true } wearer)
            return;

        if (!ent.Comp.Modes.ContainsKey(mode))
            return;

        if (!TryComp<FieldShieldComponent>(wearer, out var shieldComp))
            return;

        shieldComp.ShieldData = ent.Comp.Modes[mode];

        ent.Comp.ShieldData = ent.Comp.Modes[mode];

        ent.Comp.Mode = mode;

        Dirty(wearer, shieldComp);

        Dirty(ent);
    }
}
