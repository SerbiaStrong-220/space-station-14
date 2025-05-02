// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.SS220.MindSlave.Components;
using Content.Shared.Damage;
using Content.Shared.Implants;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.MindSlave.Systems;

public sealed class MindSlaveDisfunctionSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const float SecondsBetweenStageDamage = 4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveDisfunctionComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MindSlaveDisfunctionComponent, ComponentShutdown>(OnRemove);

        SubscribeLocalEvent<MindSlaveDisfunctionProviderComponent, ImplantImplantedEvent>(OnProviderImplanted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MindSlaveDisfunctionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active)
                return;

            if (comp.DisfunctionStage == MindSlaveDisfunctionType.Deadly
                && _gameTiming.CurTime > comp.NextDeadlyDamageTime
                && TryComp<MobStateComponent>(uid, out var stateComponent)
                && stateComponent.CurrentState != MobState.Dead)
            {
                _damageable.TryChangeDamage(uid, comp.DeadlyStageDamage, true);
                comp.NextDeadlyDamageTime = _gameTiming.CurTime + TimeSpan.FromSeconds(SecondsBetweenStageDamage);
            }

            if (_gameTiming.CurTime > comp.NextProgressTime)
            {
                ProgressDisfunction((uid, comp));
                comp.NextProgressTime = EvaluateNextProgressTime((uid, comp));
            }
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

        var disfunctionComponent = EnsureComp<MindSlaveDisfunctionComponent>(args.Implanted.Value);
        disfunctionComponent.DisfunctionParameters = entity.Comp.Disfunction;
    }

    public void ProgressDisfunction(Entity<MindSlaveDisfunctionComponent?> entity)
    {
        var (uid, comp) = entity;
        if (!Resolve(uid, ref comp))
            return;

        if (comp.DisfunctionStage == MindSlaveDisfunctionType.Deadly
            || (!comp.Deadly && comp.DisfunctionStage == MindSlaveDisfunctionType.Terminal))
            return;

        if (comp.Disfunction.TryGetValue(++comp.DisfunctionStage, out var disfunctionList))
        {
            foreach (var compName in disfunctionList)
            {
                var disfunctionComponent = _component.GetComponent(_component.GetRegistration(compName).Type);
                AddComp(uid, disfunctionComponent);
                comp.DisfunctionComponents.Add(disfunctionComponent);
            }
        }
        var progressMessage = Loc.GetString(comp.DisfunctionParameters.ProgressionPopup);
        _popup.PopupEntity(progressMessage, entity, entity, Shared.Popups.PopupType.SmallCaution);
        comp.Weakened = false;

        if (!_mind.TryGetMind(entity, out _, out var mindComponent))
            return;

        if (!_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
            return;

        _chat.ChatMessageToOne(Shared.Chat.ChatChannel.Emotes, progressMessage, progressMessage,
                                default, false, session.Channel, colorOverride: Color.Red);
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

        if (comp.DisfunctionStage == MindSlaveDisfunctionType.Terminal)
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
