// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.FieldShield;

public sealed class FieldShieldProviderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const int ForceFieldPushPriority = 2;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldShieldComponent, ExaminedEvent>(OnFieldShieldExamined);
        SubscribeLocalEvent<FieldShieldProviderComponent, ExaminedEvent>(OnFieldShieldProviderExamined);

        SubscribeLocalEvent<FieldShieldProviderComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);

        SubscribeLocalEvent<FieldShieldProviderComponent, GotEquippedEvent>(OnProviderEquipped);
        SubscribeLocalEvent<FieldShieldProviderComponent, GotUnequippedEvent>(OnProviderUnequipped);

        SubscribeLocalEvent<FieldShieldComponent, BeforeDamageChangedEvent>(OnFieldShieldBeforeDamage);
        SubscribeLocalEvent<FieldShieldComponent, DamageModifyEvent>(OnShieldDamageModify);
    }

    private void OnFieldShieldExamined(Entity<FieldShieldComponent> entity, ref ExaminedEvent args)
    {
        var charges = entity.Comp.RechargeStartTime + entity.Comp.RechargeShieldData.RechargeTime > _gameTiming.CurTime ? entity.Comp.ShieldCharge : entity.Comp.ShieldData.ShieldMaxCharge;

        if (entity.Owner == args.Examiner)
        {
            args.PushMarkup(Loc.GetString("field-shield-self-examine", ("Charges", charges), ("MaxCharge", entity.Comp.ShieldData.ShieldMaxCharge)), ForceFieldPushPriority);
        }
        else if (charges > 0)
        {
            args.PushMarkup(Loc.GetString("field-shield-other-examine"), ForceFieldPushPriority);
        }
    }

    private void OnFieldShieldProviderExamined(Entity<FieldShieldProviderComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("field-shield-provider-examine", ("ChargeTime", entity.Comp.RechargeShieldData.RechargeTime), ("MaxCharge", entity.Comp.ShieldData.ShieldMaxCharge)));
    }

    private void OnBeingEquippedAttempt(Entity<FieldShieldProviderComponent> entity, ref BeingEquippedAttemptEvent args)
    {
        if (!HasComp<FieldShieldComponent>(args.Equipee))
            return;

        args.Cancel();

        _popup.PopupClient(Loc.GetString("field-shield-provider-cant-equip-when-you-already-have-one"), args.Equipee);
    }

    private void OnProviderEquipped(Entity<FieldShieldProviderComponent> entity, ref GotEquippedEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var shieldComp = EnsureComp<FieldShieldComponent>(args.Equipee);
        shieldComp.ShieldData = entity.Comp.ShieldData;
        shieldComp.RechargeShieldData = entity.Comp.RechargeShieldData;
        shieldComp.LightData = entity.Comp.LightData;

        shieldComp.RechargeStartTime = _gameTiming.CurTime;
        Dirty(args.Equipee, shieldComp);
    }

    private void OnProviderUnequipped(Entity<FieldShieldProviderComponent> entity, ref GotUnequippedEvent args)
    {
        RemCompDeferred<FieldShieldComponent>(args.Equipee);
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
        if (entity.Comp.ShieldCharge <= 0 || args.OriginalDamage.GetTotal() < entity.Comp.ShieldData.DamageThreshold)
            return;

        UpdateShieldTimer(entity);

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
        if (_gameTiming.CurTime > entity.Comp.RechargeStartTime + entity.Comp.RechargeShieldData.RechargeTime)
            entity.Comp.ShieldCharge = entity.Comp.ShieldData.ShieldMaxCharge;

        entity.Comp.RechargeStartTime = _gameTiming.CurTime;
        DirtyField(entity!, nameof(FieldShieldComponent.RechargeStartTime));
    }
}
