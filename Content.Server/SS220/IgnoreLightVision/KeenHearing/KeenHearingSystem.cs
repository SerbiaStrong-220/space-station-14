// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Actions;
using Content.Shared.SS220.IgnoreLightVision;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.IgnoreLightVision.KeenHearing;

/// <summary>
/// Handles enabling of KeenHearing by action
/// </summary>
public sealed class KeenHearingSystem : SharedAddIgnoreLightVisionOverlaySystem<KeenHearingComponent>
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private EntProtoId _actionProto = "ActionToggleKeenHearing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeenHearingComponent, UseKeenHearingEvent>(OnKeenHearingAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entityQuery = EntityQueryEnumerator<KeenHearingComponent>();

        while (entityQuery.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime > comp.ToggleTime)
            {
                Toggle((uid, comp));
                comp.ToggleTime = null;
            }
        }

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
            if (action.Comp.BaseEvent is UseKeenHearingEvent)
                actionsToDelete.Add(action.Id);

        foreach (var action in actionsToDelete)
            _actions.RemoveAction(action);
    }

    private void OnKeenHearingAction(Entity<KeenHearingComponent> ent, ref UseKeenHearingEvent args)
    {
        Toggle((ent.Owner, ent.Comp));

        if (args.Duration != null)
            ent.Comp.ToggleTime = _gameTiming.CurTime + TimeSpan.FromSeconds(args.Duration.Value);

        args.Handled = true;
    }
}
