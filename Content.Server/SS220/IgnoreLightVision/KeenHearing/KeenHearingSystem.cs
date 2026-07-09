// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.SS220.IgnoreLightVision;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.IgnoreLightVision.KeenHearing;

/// <summary>
/// Handles enabling of KeenHearing by action
/// </summary>
public sealed partial class KeenHearingSystem : SharedAddIgnoreLightVisionOverlaySystem<KeenHearingComponent>
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    private readonly EntProtoId _actionProto = "ActionToggleKeenHearing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeenHearingComponent, UseKeenHearingEvent>(OnKeenHearingAction);
        SubscribeLocalEvent<KeenHearingComponent, ComponentStartup>(OnKeenStartup);
    }

    private void OnKeenStartup(Entity<KeenHearingComponent> ent, ref ComponentStartup args)
    {
        RefreshKeenHearing((ent.Owner, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entityQuery = EntityQueryEnumerator<KeenHearingComponent>();

        while (entityQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ToggleTime is null)
                continue;

            if (_gameTiming.CurTime <= comp.ToggleTime)
                continue;

            comp.ManualOn = !comp.ManualOn;
            comp.ToggleTime = null;
            RefreshKeenHearing((uid, comp));
        }
    }

    public void RefreshKeenHearing(Entity<KeenHearingComponent> ent)
    {
        var ev = new GetKeenHearingModifiersEvent();
        RaiseLocalEvent(ent, ref ev);

        var target = ent.Comp.ManualOn || ev.ForceOn
            ? IgnoreLightVisionOverlayState.Half
            : IgnoreLightVisionOverlayState.Off;

        if (ent.Comp.State == target)
            return;

        ent.Comp.State = target;
        Dirty(ent);
    }

    protected override void OnMapInit(Entity<KeenHearingComponent> ent, ref MapInitEvent args)
    {
        base.OnMapInit(ent, ref args);
        if (ent.Comp.AddAction)
            _actions.AddAction(ent.Owner, _actionProto);
    }

    protected override void OnComponentRemove(Entity<KeenHearingComponent> ent, ref ComponentRemove args)
    {
        base.OnComponentRemove(ent, ref args);

        if (!ent.Comp.AddAction)
            return;

        List<EntityUid> actionsToDelete = [];

        foreach (var action in _actions.GetActions(ent.Owner))
        {
            if (TryComp<InstantActionComponent>(action.Owner, out var instantAction)
                && instantAction.Event is UseKeenHearingEvent)
                actionsToDelete.Add(action.Owner);
        }

        foreach (var action in actionsToDelete)
        {
            _actions.RemoveAction(action);
        }
    }

    private void OnKeenHearingAction(Entity<KeenHearingComponent> ent, ref UseKeenHearingEvent args)
    {
        ent.Comp.ManualOn = !ent.Comp.ManualOn;
        RefreshKeenHearing((ent.Owner, ent.Comp));

        if (args.Duration != null)
            ent.Comp.ToggleTime = _gameTiming.CurTime + TimeSpan.FromSeconds(args.Duration.Value);

        args.Handled = true;
    }
}
