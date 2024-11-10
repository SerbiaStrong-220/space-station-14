// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Server.SS220.MindSlave.Components;
using Content.Shared.Damage;
using Content.Shared.Implants;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.MindSlave.Systems;

public sealed class MindSlaveDisfunctionSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float SecondsBetweenStageDamage = 4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveDisfunctionComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MindSlaveDisfunctionComponent, ComponentShutdown>(OnRemove);

        SubscribeLocalEvent<MindSlaveDisfunctionProviderComponent, ImplantImplantedEvent>(OnProviderImplanted);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<MindSlaveDisfunctionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active)
                return;

            if (comp.DisfunctionStage == MindSlaveDisfunctionType.deadly
                && _gameTiming.CurTime > comp.NextDeadlyDamageTime)
            {
                _damageable.TryChangeDamage(uid, comp.DeadlyStageDamage, true);
                comp.NextDeadlyDamageTime = _gameTiming.CurTime + TimeSpan.FromSeconds(SecondsBetweenStageDamage);
            }

            if (_gameTiming.CurTime > comp.NextProgressTime)
                ProgressDisfunction((uid, comp));
        }
    }

    private void OnInit(Entity<MindSlaveDisfunctionComponent> entity, ref MapInitEvent _)
    {
        entity.Comp.NextProgressTime = EvaluateNextProgressTime(entity);
    }

    private void OnRemove(Entity<MindSlaveDisfunctionComponent> entity, ref ComponentShutdown _)
    {
        // lets help admins a little
        foreach (var comp in entity.Comp.DisfunctionComponents)
        {
            RemComp(entity.Owner, comp);
        }
    }

    private void OnProviderImplanted(Entity<MindSlaveDisfunctionProviderComponent> entity, ref ImplantImplantedEvent args)
    {
        if (args.Implanted == null)
            return;

        var disfunctionComponent = EnsureComp<MindSlaveDisfunctionComponent>(entity);
        disfunctionComponent.DisfunctionParameters = entity.Comp.Disfunction;
    }

    public void ProgressDisfunction(Entity<MindSlaveDisfunctionComponent?> entity)
    {
        var (uid, comp) = entity;
        if (!Resolve(uid, ref comp))
            return;

        if (comp.DisfunctionStage == MindSlaveDisfunctionType.terminal
            && (!comp.Deadly || comp.DisfunctionStage == MindSlaveDisfunctionType.terminal))
            return;

        foreach (var compName in comp.Disfunction[++comp.DisfunctionStage])
        {
            var disfunctionComponent = _component.GetComponent(_component.GetRegistration(compName).Type);
            AddComp(uid, disfunctionComponent);
            comp.DisfunctionComponents.Add(disfunctionComponent);
        }

        _popup.PopupEntity(comp.DisfunctionParameters.ProgressionPopup, entity, entity, Shared.Popups.PopupType.SmallCaution);
        comp.Weakened = false;
    }

    public void WeakDisfunction(Entity<MindSlaveDisfunctionComponent?> entity, float delayMinutes, int removeAmount)
    {
        var (uid, comp) = entity;
        if (!Resolve(uid, ref comp))
            return;

        comp.Weakened = true;
        comp.NextProgressTime += TimeSpan.FromMinutes(delayMinutes);

        foreach (var disfunctionComponent in _random.GetItems(comp.DisfunctionComponents, removeAmount, false))
        {
            RemComp(uid, disfunctionComponent);
        }

        if (comp.DisfunctionStage == MindSlaveDisfunctionType.terminal)
            comp.Deadly = true;
    }

    public void PauseDisfunction(Entity<MindSlaveDisfunctionComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (entity.Comp.Active == false)
        {
            Log.Error("Tried to pause mind slave disfunction, but it is already paused");
            return;
        }

        entity.Comp.Active = false;
        entity.Comp.PausedTime = _gameTiming.CurTime;
    }

    public void UnpauseDisfunction(Entity<MindSlaveDisfunctionComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (entity.Comp.Active == true)
        {
            Log.Error("Tried to unpause mind slave disfunction, but it is already active");
            return;
        }

        entity.Comp.Active = true;
        entity.Comp.NextProgressTime += _gameTiming.CurTime - entity.Comp.PausedTime;
    }

    private TimeSpan EvaluateNextProgressTime(Entity<MindSlaveDisfunctionComponent> entity)
    {
        var (_, comp) = entity;

        return _gameTiming.CurTime + TimeSpan.FromMinutes(comp.ConstMinutesBetweenStages)
                                    + TimeSpan.FromMinutes(_random.NextFloat(-1f, 1f) * comp.MaxRandomMinutesBetweenStages);
    }

}
