using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Repairable;

public sealed partial class RepairableSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializeBorgRepair()
    {
        SubscribeLocalEvent<RepairableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RepairableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RepairableComponent, BorgRepairDoAfterEvent>(OnDoAfter);
    }

    private bool UsesBorgRepair(Entity<RepairableComponent> ent)
    {
        // AllowSelfRepair is the only configuration flag. Existing chassis identification keeps
        // ordinary repairable objects on the standard repair path.
        return ent.Comp.AllowSelfRepair && HasComp<BorgChassisComponent>(ent);
    }

    private void OnMapInit(Entity<RepairableComponent> ent, ref MapInitEvent args)
    {
        if (!UsesBorgRepair(ent))
            return;

        if (TryComp<DamageableComponent>(ent, out var damageable))
        {
            var damage = _damageableSystem.GetTotalDamage((ent.Owner, damageable));
            var addedDamage = FixedPoint2.Max(damage - ent.Comp.LastDamage, 0);
            ent.Comp.UnrepairableDamage = FixedPoint2.Min(damage,
                ent.Comp.UnrepairableDamage + FixedPoint2.FromHundredths((addedDamage.Value + 1) / 2));
            ent.Comp.LastDamage = damage;
            Dirty(ent);
        }
    }

    private void OnDamageChanged(Entity<RepairableComponent> ent, ref DamageChangedEvent args)
    {
        // The server already includes the repair budget in the component state.
        if (_timing.ApplyingState || !UsesBorgRepair(ent))
            return;

        // Also account for damage set directly, which has no damage delta.
        var totalDamage = _damageableSystem.GetTotalDamage((ent.Owner, args.Damageable));
        var damageDelta = totalDamage - ent.Comp.LastDamage;

        if (damageDelta > 0)
        {
            ent.Comp.UnrepairableDamage = FixedPoint2.Min(totalDamage,
                ent.Comp.UnrepairableDamage + FixedPoint2.FromHundredths((damageDelta.Value + 1) / 2));
        }
        else if (damageDelta < 0 && (args.Origin is not { } origin || !HasComp<BorgChassisComponent>(origin)))
        {
            ent.Comp.UnrepairableDamage = FixedPoint2.Max(ent.Comp.UnrepairableDamage + damageDelta, 0);
        }

        ent.Comp.UnrepairableDamage = FixedPoint2.Min(ent.Comp.UnrepairableDamage, totalDamage);
        ent.Comp.LastDamage = totalDamage;
        Dirty(ent);
    }

    private FixedPoint2 GetRepairableDamage(Entity<RepairableComponent> ent, EntityUid user)
    {
        var damage = _damageableSystem.GetTotalDamage(ent.Owner);
        if (HasComp<BorgChassisComponent>(user))
            damage -= ent.Comp.UnrepairableDamage;

        return FixedPoint2.Max(damage, 0);
    }

    private void OnBorgInteractUsing(Entity<RepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !UsesBorgRepair(ent))
            return;

        // Own the chassis repair interaction, including failed attempts at the borg repair limit.
        if (!_toolSystem.HasQuality(args.Used, ent.Comp.QualityNeeded))
            return;

        args.Handled = true;
        if (GetRepairableDamage(ent, args.User) <= 0)
        {
            if (_damageableSystem.GetTotalDamage(ent.Owner) > 0)
                _popup.PopupClient(Loc.GetString("borg-repair-human-required"), ent, args.User);
            return;
        }

        StartRepair(ent, args.User, args.Used);
    }

    private bool StartRepair(Entity<RepairableComponent> ent, EntityUid user, EntityUid tool)
    {
        if (!UsesBorgRepair(ent))
            return false;

        if (ent.Comp.DamageValue is not { } damageValue || damageValue >= 0)
            return false;

        var damagePerRepair = FixedPoint2.New(-damageValue);
        if (damagePerRepair <= 0)
            return false;

        var amount = FixedPoint2.Min(GetRepairableDamage(ent, user), damagePerRepair);
        if (amount <= 0)
            return false;

        // Charge only for this portion, including the last partial portion at the repair limit.
        var fraction = amount.Float() / damagePerRepair.Float();
        var delay = ent.Comp.DoAfterDelay * fraction;
        if (user == ent.Owner)
            delay *= ent.Comp.SelfRepairPenalty;

        return _toolSystem.UseTool(tool, user, ent.Owner, delay, ent.Comp.QualityNeeded,
            new BorgRepairDoAfterEvent(amount), ent.Comp.FuelCost * fraction);
    }

    private void OnDoAfter(Entity<RepairableComponent> ent, ref BorgRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } tool ||
            !UsesBorgRepair(ent))
            return;

        args.Handled = true;
        var amount = FixedPoint2.Min(args.Amount, GetRepairableDamage(ent, args.User));
        if (amount <= 0)
            return;

        var changed = _damageableSystem.HealEvenly(ent.Owner, -amount, origin: args.User);
        _adminLogger.Add(LogType.Healed,
            $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} by {changed.GetTotal()}");

        // Start a fresh tool use so both time and fuel are recalculated from the remaining damage.
        if (ent.Comp.AutoDoAfter && GetRepairableDamage(ent, args.User) > 0 &&
            Exists(tool) && StartRepair(ent, args.User, tool))
            return;

        var message = GetRepairableDamage(ent, args.User) <= 0 && _damageableSystem.GetTotalDamage(ent.Owner) > 0
            ? Loc.GetString("borg-repair-human-required")
            : Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", tool));
        _popup.PopupClient(message, ent, args.User);

        var ev = new RepairedEvent(ent, args.User);
        RaiseLocalEvent(ent.Owner, ref ev);
    }
}

[Serializable, NetSerializable]
public sealed partial class BorgRepairDoAfterEvent : DoAfterEvent
{
    public readonly FixedPoint2 Amount;

    public BorgRepairDoAfterEvent(FixedPoint2 amount)
    {
        Amount = amount;
    }

    public override DoAfterEvent Clone() => new BorgRepairDoAfterEvent(Amount);
}
