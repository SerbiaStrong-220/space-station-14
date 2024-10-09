using Content.Server.SS220.DarkForces.Saint.Items.Cross.Events;
using Content.Server.SS220.DarkForces.Saint.Items.Events;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Saint.Items.Cross;

public sealed partial class SaintCrossSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SaintCrossComponent, ComponentInit>(OnSaintCrossInit);
        SubscribeLocalEvent<SaintCrossComponent, OnItemSainted>(OnItemSainted);

        // InitializeVampire();
        InitializeDamage();
    }

    private void OnItemSainted(EntityUid uid, SaintCrossComponent component, OnItemSainted args)
    {
        component.Sainted = true;
        component.NextTickToUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(15);
    }

    private void OnSaintCrossInit(EntityUid uid, SaintCrossComponent component, ComponentInit args)
    {
        component.NextTickToUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(15);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<SaintCrossComponent>();
        while (query.MoveNext(out var cross, out var saintCrossComponent))
        {
            if (!saintCrossComponent.Sainted || saintCrossComponent.NextTickToUpdate > curTime)
                continue;

            var ev = new SaintedCrossFindingEvent(cross);
            RaiseLocalEvent(ev);

            if (ev.Handled)
            {
                OnSaintCrossHandledFinding(cross, ev);
            }

            else
            {
                _pointLight.SetEnabled(cross, false);
            }

            saintCrossComponent.NextTickToUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(15);
        }
    }

    private void OnSaintCrossHandledFinding(EntityUid uid, SaintedCrossFindingEvent args)
    {
        if (args.Message != null)
        {
            _popupSystem.PopupEntity(args.Message.Value.Message, uid, PopupType.Medium);
        }

        if (args.Colorize == null)
        {
            _pointLight.SetEnabled(uid, false);
            return;
        }

        var colorize = args.Colorize.Value;

        _pointLight.SetEnabled(uid, true);
        _pointLight.SetColor(uid, colorize.Color);
        _pointLight.SetEnergy(uid, colorize.Energy);
        _pointLight.SetRadius(uid, colorize.Radius);
    }
}
