// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.LyingDown;

public sealed partial class LyingDownSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    private string LieDownAction = "ActionLieDown";

    private string StandUpAction = "ActionStandUp";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LyingDownComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LyingDownComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LyingDownComponent, LieDownActionEvent>(OnLyingAction);
        SubscribeLocalEvent<LyingDownComponent, StandUpActionEvent>(OnStandUpAction);
        SubscribeLocalEvent<LyingDownComponent, UpdateCanMoveEvent>(OnCanMoveUpdate);
        SubscribeLocalEvent<LyingDownComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStartup(Entity<LyingDownComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.ActionUid = _actions.AddAction(entity, LieDownAction);
        Dirty(entity);
    }

    private void OnShutdown(Entity<LyingDownComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.IsLying)
            StandUp(entity);

        if (entity.Comp.ActionUid != null)
        {
            _actions.RemoveAction(entity.Comp.ActionUid);
            entity.Comp.ActionUid = null;
        }

        Dirty(entity);
    }

    private void OnLyingAction(Entity<LyingDownComponent> entity, ref LieDownActionEvent args)
    {
        if (args.Handled ||
            !_mobState.IsAlive(entity))
            return;

        LieDown(entity);
        args.Handled = true;
    }

    private void OnStandUpAction(Entity<LyingDownComponent> entity, ref StandUpActionEvent args)
    {
        if (args.Handled)
            return;

        StandUp(entity);
        args.Handled = true;
    }

    private void OnCanMoveUpdate(Entity<LyingDownComponent> entity, ref UpdateCanMoveEvent args)
    {
        if (entity.Comp.IsLying)
            args.Cancel();
    }

    private void OnMobStateChanged(Entity<LyingDownComponent> entity, ref MobStateChangedEvent args)
    {
        if (entity.Comp.IsLying &&
            args.NewMobState is not MobState.Alive)
            StandUp(entity);
    }

    private void LieDown(EntityUid uid, LyingDownComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _actions.RemoveAction(component.ActionUid);
        component.ActionUid = _actions.AddAction(uid, StandUpAction);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, LyingVisuals.State, true, appearance);

        component.IsLying = true;
        Dirty(uid, component);

        _actionBlocker.UpdateCanMove(uid);
    }

    private void StandUp(EntityUid uid, LyingDownComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _actions.RemoveAction(component.ActionUid);
        component.ActionUid = _actions.AddAction(uid, LieDownAction);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, LyingVisuals.State, false, appearance);

        component.IsLying = false;
        Dirty(uid, component);

        _actionBlocker.UpdateCanMove(uid);
    }
}

public sealed partial class LieDownActionEvent : InstantActionEvent
{
}

public sealed partial class StandUpActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum LyingVisuals : byte
{
    State,
    Lying
}

