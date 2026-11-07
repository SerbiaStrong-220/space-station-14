using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;
using Content.Server.Power.Components;
using Content.Server.Wires;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.SS220.DarkForces.Ratvar.Events;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Gear;

public sealed class RatvarGearSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly RatvarProgressSystem _progressSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly WiresSystem _wiresSystem = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string SmokeEffect = "RatvarSmokeEffect";

    private const int MaxGearPower = 300;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarGearTargetComponent, ComponentInit>(OnTargetInit);
        SubscribeLocalEvent<RatvarGearTargetComponent, ComponentShutdown>(OnTargetShutdown);
        SubscribeLocalEvent<RatvarGearTargetComponent, ContainerIsInsertingAttemptEvent>(OnInsertGear);
        SubscribeLocalEvent<RatvarGearTargetComponent, ExaminedEvent>(OnExaminedEvent);

        SubscribeLocalEvent<RatvarGearComponent, AfterInteractEvent>(OnAfterInteractEvent);
        SubscribeLocalEvent<RatvarGearComponent, RatvarGearOpenPanelDoAfterEvent>(OnOpenPanelEvent);
        SubscribeLocalEvent<RatvarGearComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveGear);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<RatvarGearComponent, TransformComponent>();
        while (query.MoveNext(out _, out var component, out var transformComponent))
        {
            if (!component.Active || component.Power >= MaxGearPower)
                continue;

            if (component.NextTick > curTime)
                continue;

            _progressSystem.TryRequestChangePower(component.PowerPerTick);
            component.NextTick = curTime + TimeSpan.FromSeconds(10);
            component.Power += component.PowerPerTick;

            Spawn(SmokeEffect, transformComponent.Coordinates);
        }
    }

    private void OnExaminedEvent(EntityUid uid, RatvarGearTargetComponent component, ExaminedEvent args)
    {
        var hasGear = _slotsSystem.GetItemOrNull(uid, component.GearSlotId) != null;
        if (hasGear)
            args.PushMarkup("[color=#b87333]\u2699\u2699\u2699 В нем что-то из латуни \u2699\u2699\u2699[/color]");
    }

    private void OnRemoveGear(EntityUid uid, RatvarGearComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (HasComp<RatvarGearTargetComponent>(args.Container.Owner))
            QueueDel(uid);
    }

    private void OnTargetInit(EntityUid uid, RatvarGearTargetComponent component, ComponentInit args)
    {
        _slotsSystem.AddItemSlot(uid, component.GearSlotId, component.GearSlot);
    }

    private void OnTargetShutdown(EntityUid uid, RatvarGearTargetComponent component, ComponentShutdown args)
    {
        _slotsSystem.RemoveItemSlot(uid, component.GearSlot);
    }

    private void OnInsertGear(EntityUid uid, RatvarGearTargetComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!TryComp<RatvarGearComponent>(args.EntityUid, out var ratvarGearComponent) ||
            !TryComp<WiresPanelComponent>(uid, out var wirePanel) || !wirePanel.Open)
        {
            args.Cancel();
            return;
        }

        ratvarGearComponent.Active = true;
        ratvarGearComponent.NextTick = _timing.CurTime + TimeSpan.FromSeconds(10);
        _slotsSystem.SetLock(uid, component.GearSlot, true);
    }

    private void OnOpenPanelEvent(EntityUid uid, RatvarGearComponent component, RatvarGearOpenPanelDoAfterEvent args)
    {
        var target = args.Target;
        if (target == null || !TryComp<WiresPanelComponent>(target.Value, out var wiresPanel))
            return;

        _wiresSystem.TogglePanel(target.Value, wiresPanel, true);
    }

    private void OnAfterInteractEvent(EntityUid uid, RatvarGearComponent component, AfterInteractEvent args)
    {
        var target = args.Target;
        if (target == null || !HasComp<ApcComponent>(target) ||
            !TryComp<WiresPanelComponent>(target, out var wirePanel))
            return;

        if (wirePanel.Open)
            return;

        var openPanelDoAfter = new RatvarGearOpenPanelDoAfterEvent();
        var openPanelDoAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            TimeSpan.FromSeconds(4),
            openPanelDoAfter,
            uid,
            target,
            uid
        )
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 1f
        };

        _doAfterSystem.TryStartDoAfter(openPanelDoAfterArgs);
        args.Handled = true;
    }
}
