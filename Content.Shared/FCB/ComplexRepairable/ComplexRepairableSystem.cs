// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.FCB.ComplexRepairable;

public sealed partial class ComplexRepairableSystem : EntitySystem
{
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
        if(args.Damageable.TotalDamage >= ent.Comp.MaterialRepairTreshold)
        {
            ent.Comp.LeftToInsert = (args.Damageable.TotalDamage / ent.Comp.MaterialRepairTreshold).Int();
        }

        Dirty(ent);
    }

    private void OnRepairFinished(Entity<ComplexRepairableComponent> ent,  ref ComplexRepairFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<DamageableComponent>(ent.Owner, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
            return;

        if (ent.Comp.Damage != null)
        {
            var damageChanged = _damageableSystem.TryChangeDamage(ent.Owner, ent.Comp.Damage, true, false, origin: args.User);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} by {ent.Comp.Damage.GetTotal()}");
        }

        else
        {
            // Repair all damage
            _damageableSystem.SetAllDamage(ent.Owner, damageable, 0);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent.Owner):target} back to full health");
        }

        var str = Loc.GetString("comp-repairable-repair", ("target", ent.Owner), ("tool", args.Used!));
        _popup.PopupClient(str, ent.Owner, args.User);

        var ev = new ComplexRepairedEvent(ent, args.User);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void Repair(Entity<ComplexRepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        //if(args.Used.Proto)

        // Only try repair the target if it is damaged
        if (!TryComp<DamageableComponent>(ent.Owner, out var damageable) || damageable.TotalDamage == 0)
            return;

        if (ent.Comp.LeftToInsert > 0 )
        {
            if (!TryComp<MetaDataComponent>(args.Used, out var metaData) || metaData.EntityPrototype == null)
                return;

            if (ent.Comp.Material.Id != metaData.EntityPrototype.ID)
                return;

            if(!TryComp<StackComponent>(args.Used, out var stackComp))
            {
                _transform.DetachEntity(args.Used, Transform(args.Used));
                QueueDel(args.Used);
                --ent.Comp.LeftToInsert;
                Dirty(ent);
                var str = Loc.GetString("complex-repairable-material-repair", ("target", ent.Owner), ("material", args.Used!));
                _popup.PopupClient(str, ent.Owner, args.User);
                return;
            }

            if(stackComp.Count > ent.Comp.LeftToInsert)
            {
                _stack.SetCount(args.Used, stackComp.Count - ent.Comp.LeftToInsert);
                ent.Comp.LeftToInsert = 0;
                Dirty(ent);
                var str = Loc.GetString("complex-repairable-material-repair", ("target", ent.Owner), ("material", args.Used!));
                _popup.PopupClient(str, ent.Owner, args.User);
                return;
            }
            ent.Comp.LeftToInsert = ent.Comp.LeftToInsert - stackComp.Count;
            _stack.SetCount(args.Used, 0);
        }

        if (ent.Comp.LeftToInsert > 0)
        {
            Dirty(ent);
            return;
        }

        float delay = ent.Comp.DoAfterModifier * (damageable.TotalDamage.Int() / 10);

        // Add a penalty to how long it takes if the user is repairing itself
        if (args.User == args.Target)
        {
            if (!ent.Comp.AllowSelfRepair)
                return;

            delay *= ent.Comp.SelfRepairPenalty;
        }

        // Run the repairing doafter
        args.Handled = _toolSystem.UseTool(args.Used, args.User, ent.Owner, delay, ent.Comp.QualityNeeded, new ComplexRepairFinishedEvent(), ent.Comp.FuelCost);
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
