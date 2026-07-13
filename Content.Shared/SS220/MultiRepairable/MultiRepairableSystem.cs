// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public sealed class MultiRepairableSystem : EntitySystem
{
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    private static readonly LocId RepairableRepair = "comp-repairable-repair";
    private static readonly LocId Target = "target";
    private static readonly LocId Tool = "tool";

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

            if (!TryGetRepairDelay(ent, args.User, option, out var delay))
                return;

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

        var str = Loc.GetString(RepairableRepair, (Target, ent), (Tool, args.Args.Used.Value));
        _popup.PopupClient(str, ent, args.User);

        var ev = new MultiRepairedEvent(ent, args.User, option);
        RaiseLocalEvent(ent, ref ev);

        args.Handled = true;

        var totalDamage = _damageableSystem.GetTotalDamage(ent.Owner);
        var toolEntity = args.Args.Used.Value;
        var toolExists = Exists(toolEntity);

        if (totalDamage > 0 && toolExists)
        {
            if (!TryGetRepairDelay(ent, args.User, option, out var delay))
                return;

            _toolSystem.UseTool(
                toolEntity,
                args.User,
                ent,
                delay,
                option.QualityNeeded,
                new MultiRepairDoAfterEvent(option),
                option.FuelCost
            );
        }
    }

    private bool TryGetRepairDelay(Entity<MultiRepairableComponent> ent, EntityUid user, RepairOption option, out float delay)
    {
        delay = option.DoAfterDelay;

        if (user == ent.Owner)
        {
            if (!ent.Comp.AllowSelfRepair)
                return false;

            delay *= ent.Comp.SelfRepairPenalty;
        }

        return true;
    }
}

[ByRefEvent]
public readonly record struct MultiRepairedEvent(EntityUid Target, EntityUid User, RepairOption Option);

[Serializable, NetSerializable]
public sealed partial class MultiRepairDoAfterEvent : DoAfterEvent
{
    public readonly RepairOption RepairOption;

    public MultiRepairDoAfterEvent(RepairOption option)
    {
        RepairOption = option;
    }

    public override DoAfterEvent Clone() => this;
}