using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
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

    private void OnInteractUsing(EntityUid uid, MultiRepairableComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        // 1. Проверяем, нужно ли вообще чинить
        if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage == 0)
            return;

        foreach (var option in component.Options)
        {
            // Проверяем, подходит ли инструмент (качество)
            if (!_toolSystem.HasQuality(args.Used, option.QualityNeeded))
                continue;

            var delay = option.DoAfterDelay;
            if (args.User == uid)
            {
                if (!component.AllowSelfRepair) return;
                delay *= component.SelfRepairPenalty;
            }

            // 2. Используем UseTool вместо прямого DoAfter. 
            // Это автоматически проверит топливо (FuelCost) и запустит прогресс-бар.
            args.Handled = _toolSystem.UseTool(
                args.Used, 
                args.User, 
                uid, 
                delay, 
                option.QualityNeeded, 
                new MultiRepairDoAfterEvent(option), 
                option.FuelCost);
            
            if (args.Handled)
                break;
        }
    }

    private void OnDoAfter(EntityUid uid, MultiRepairableComponent component, MultiRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null) return;

        var option = args.RepairOption;
        
        // 3. Применяем лечение
        if (option.Damage != null)
        {
            var damageChanged = _damageableSystem.TryChangeDamage(uid, option.Damage, true, origin: args.User);
            
            // Админ-лог для истории
            _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()} using {ToPrettyString(args.Args.Used.Value):tool}");
        }

        // Сообщение игроку
        var str = Loc.GetString("comp-repairable-repair", ("target", uid), ("tool", args.Args.Used.Value));
        _popup.PopupClient(str, uid, args.User);

        // Вызываем событие об успешном ремонте для других систем
        var ev = new MultiRepairedEvent(uid, args.User, option);
        RaiseLocalEvent(uid, ref ev);

        args.Handled = true;
    }
}

// Обновленное событие с данными о выбранной опции
[ByRefEvent]
public readonly record struct MultiRepairedEvent(EntityUid Target, EntityUid User, RepairOption Option);

[Serializable, NetSerializable]
public sealed partial class MultiRepairDoAfterEvent : DoAfterEvent
{
    public readonly RepairOption RepairOption;
    public MultiRepairDoAfterEvent(RepairOption option) => RepairOption = option;
    public override DoAfterEvent Clone() => this;
}