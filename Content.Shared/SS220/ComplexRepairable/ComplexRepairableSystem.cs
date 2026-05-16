// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ComplexRepairable;

public sealed partial class ComplexRepairableSystem : EntitySystem
{
    private static readonly string MaterialRepair = "complex-repairable-material-repair";
    private static readonly string RepairDone = "comp-repairable-repair";

    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ComplexRepairableComponent, InteractUsingEvent>(Repair);
        SubscribeLocalEvent<ComplexRepairableComponent, ComplexRepairFinishedEvent>(OnRepairFinished);
        SubscribeLocalEvent<ComplexRepairableComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<ComplexRepairableComponent> ent, ref DamageChangedEvent args)
    {
        var damageTaken = args.DamageDelta?.GetTotal() ?? FixedPoint2.Zero;

        if (damageTaken > 0 && ent.Comp.MaterialRepairTreshold != 0)
            ent.Comp.LeftToInsert += (damageTaken / ent.Comp.MaterialRepairTreshold).Int();

        DirtyField(ent.AsNullable(), nameof(ComplexRepairableComponent.LeftToInsert));
    }

    private void OnRepairFinished(Entity<ComplexRepairableComponent> ent,  ref ComplexRepairFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        if (_damageableSystem.GetTotalDamage(ent.Owner) == 0)
            return;

        if (ent.Comp.Damage != null)
        {
            var damageChanged = _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.Damage, true, false, origin: args.User);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} by {ent.Comp.Damage.GetTotal()}");
        }

        else
        {
            // Repair all damage
            _damageableSystem.SetAllDamage(ent.Owner, 0);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} back to full health");
        }

        var str = Loc.GetString(RepairDone, ("target", ent.Owner), ("tool", args.Used!));
        _popup.PopupClient(str, ent.Owner, args.User);

        var ev = new ComplexRepairedEvent(ent, args.User);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void Repair(Entity<ComplexRepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Only try repair the target if it is damaged
        if (_damageableSystem.GetTotalDamage(ent.Owner) == 0)
            return;

        if (ent.Comp.LeftToInsert > 0)
        {
            if (MetaData(args.Used).EntityPrototype is not { } entityProto)
                return;

            if (ent.Comp.Material.Id != entityProto.ID)
                return;

            if (!TryComp<StackComponent>(args.Used, out var stackComp))
            {
                QueueDel(args.Used);
                ent.Comp.LeftToInsert -= 1;
            }

            if (stackComp != null)
            {
                int toBeUsed = ent.Comp.LeftToInsert;

                if (stackComp.Count < ent.Comp.LeftToInsert)
                    toBeUsed = stackComp.Count;

                _stack.TryUse(args.Used, toBeUsed);

                ent.Comp.LeftToInsert -= toBeUsed;
            }

            Dirty(ent);
            var str = Loc.GetString(MaterialRepair, ("target", ent.Owner), ("material", args.Used!));
            _popup.PopupClient(str, ent.Owner, args.User);
            return;
        }

        if (ent.Comp.LeftToInsert > 0)
        {
            Dirty(ent);
            return;
        }

        float delay = ent.Comp.DoAfterModifier.Float() * (_damageableSystem.GetTotalDamage(ent.Owner).Float() / 10f);

        // Add a penalty to how long it takes if the user is repairing itself
        if (args.User == args.Target)
        {
            if (!ent.Comp.AllowSelfRepair)
                return;

            delay *= ent.Comp.SelfRepairPenalty;
        }

        // Run the repairing doafter
        args.Handled = _toolSystem.UseTool(args.Used, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new ComplexRepairFinishedEvent(), ent.Comp.FuelCost.Float());
    }
}

/// <summary>
/// Event raised on an entity when its successfully repaired.
/// </summary>
/// <param name="Ent"></param>
/// <param name="User"></param>
[ByRefEvent]
public readonly record struct ComplexRepairedEvent(Entity<ComplexRepairableComponent> Ent, EntityUid User);

[Serializable, NetSerializable]
public sealed partial class ComplexRepairFinishedEvent : SimpleDoAfterEvent;
