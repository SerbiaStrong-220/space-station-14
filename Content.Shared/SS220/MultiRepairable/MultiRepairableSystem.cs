using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public sealed class MultiRepairableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MultiRepairableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MultiRepairableComponent, MultiRepairDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<MultiRepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_damageableSystem.GetTotalDamage(ent.Owner) == 0)
            return;

        foreach (var option in ent.Comp.Options)
        {
            if (!_toolSystem.HasQuality(args.Used, option.QualityNeeded))
                continue;

            var delay = option.DoAfterDelay;
            if (args.User == ent.Owner)
            {
                if (!ent.Comp.AllowSelfRepair)
                    return;
                delay *= ent.Comp.SelfRepairPenalty;
            }

            args.Handled = _toolSystem.UseTool(
                args.Used,
                args.User,
                ent,
                delay,
                option.QualityNeeded,
                new MultiRepairDoAfterEvent(option),
                option.FuelCost
            );

            if (args.Handled)
                break;
        }
    }

    private void OnDoAfter(Entity<MultiRepairableComponent> ent, ref MultiRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        var option = args.RepairOption;

        if (option.Damage != null)
        {
            _damageableSystem.TryChangeDamage(ent.Owner, option.Damage, true, origin: args.User);
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(ent):target} by {_damageableSystem.GetTotalDamage(ent.Owner)} using {ToPrettyString(args.Args.Used.Value):tool}");
        }

        var str = Loc.GetString("comp-repairable-repair", ("target", ent), ("tool", args.Args.Used.Value));
        _popup.PopupClient(str, ent, args.User);

        var ev = new MultiRepairedEvent(ent, args.User, option);
        RaiseLocalEvent(ent, ref ev);

        args.Handled = true;
    }
}

[ByRefEvent]
public readonly record struct MultiRepairedEvent(EntityUid Target, EntityUid User, RepairOption Option);

[Serializable, NetSerializable]
public sealed partial class MultiRepairDoAfterEvent : DoAfterEvent
{
    public readonly RepairOption RepairOption;
    public MultiRepairDoAfterEvent(RepairOption option) => RepairOption = option;
    public override DoAfterEvent Clone() => this;
}