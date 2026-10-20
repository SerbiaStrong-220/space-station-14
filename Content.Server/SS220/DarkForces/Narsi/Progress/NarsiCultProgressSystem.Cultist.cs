using System.Linq;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Narsi.Progress;

public sealed partial class NarsiCultProgressSystem
{
    private bool _leaderSelectionPending;

    private void InitializeCultists()
    {
        SubscribeLocalEvent<NarsiCultistComponent, ComponentInit>(OnCultistInit);
        SubscribeLocalEvent<NarsiCultistComponent, ComponentStartup>(OnCultistStartup);
        SubscribeLocalEvent<NarsiCultistComponent, ComponentShutdown>(OnCultistShutdown);
        SubscribeLocalEvent<NarsiCultistLeaderComponent, MobStateChangedEvent>(OnLeaderMobStateChanged);
        SubscribeLocalEvent<NarsiCultistComponent, MobStateChangedEvent>(OnCultistMobStateChanged);
    }

    private void OnCultistInit(EntityUid uid, NarsiCultistComponent component, ComponentInit args)
    {
        AddNarsiObjectives(uid);
    }

    private void OnCultistStartup(EntityUid uid, NarsiCultistComponent component, ComponentStartup args)
    {
        _leaderSelectionPending = true;
    }

    private void OnCultistShutdown(EntityUid uid, NarsiCultistComponent component, ComponentShutdown args)
    {
        if (_activeProgress is not { } progress || progress.Comp.LeaderEntity != uid)
            return;

        progress.Comp.LeaderEntity = null;
        progress.Comp.LeaderState = LeaderState.NoCandidates;
        RemCompDeferred<NarsiCultistLeaderComponent>(uid);
        _leaderSelectionPending = true;
    }

    private void UpdateCultistLeader()
    {
        if (!_leaderSelectionPending)
            return;

        _leaderSelectionPending = false;
        FindNewCultistLeader();
    }

    private void OnCultistMobStateChanged(EntityUid uid, NarsiCultistComponent component, MobStateChangedEvent args)
    {
        if (_activeProgress is not { } progress)
            return;

        if (args.NewMobState == MobState.Alive && progress.Comp.LeaderState == LeaderState.NoCandidates)
            _leaderSelectionPending = true;
    }

    private void OnLeaderMobStateChanged(EntityUid uid, NarsiCultistLeaderComponent component,
        MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (_activeProgress == null)
            return;

        var progressComponent = _activeProgress.Value.Comp;
        progressComponent.LeaderState = LeaderState.Dead;

        _leaderSelectionPending = true;
    }

    public void FindNewCultistLeader()
    {
        if (_activeProgress is not {} progress)
            return;

        if (progress.Comp.LeaderState == LeaderState.Selected &&
            progress.Comp.LeaderEntity is { } current &&
            !TerminatingOrDeleted(current) &&
            HasComp<NarsiCultistComponent>(current) &&
            HasComp<NarsiCultistLeaderComponent>(current) &&
            TryComp<MobStateComponent>(current, out var currentState) &&
            currentState.CurrentState != MobState.Dead)
            return;

        var aliveCultists = new List<EntityUid>();
        var query = EntityQueryEnumerator<NarsiCultistComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var cultist, out var mob))
        {
            if (mob.CurrentState == MobState.Alive && !TerminatingOrDeleted(uid) &&
                cultist.LifeStage == ComponentLifeStage.Running)
                aliveCultists.Add(uid);
        }

        if (progress.Comp.LeaderEntity is { } previous && !TerminatingOrDeleted(previous))
        {
            RemComp<NarsiCultistLeaderComponent>(previous);
        }

        if (aliveCultists.Count == 0)
        {
            progress.Comp.LeaderEntity = null;
            progress.Comp.LeaderState = LeaderState.NoCandidates;
            return;
        }

        var newLeader = _random.Pick(aliveCultists);
        SetNewCultistLeader(newLeader, progress);
    }

    private void SetNewCultistLeader(EntityUid leader, Entity<NarsiCultProgressComponent> progress)
    {
        if (progress.Comp.LeaderEntity is { } previous && previous != leader && !TerminatingOrDeleted(previous))
            RemComp<NarsiCultistLeaderComponent>(previous);

        progress.Comp.LeaderEntity = leader;
        progress.Comp.LeaderState = LeaderState.Selected;

        EnsureComp<NarsiCultistLeaderComponent>(leader);
        SendMessageFromNarsi(progress, "Назначен новый лидер культа!");
    }

    public void SetNewCultistLeader(EntityUid uid)
    {
        if (_activeProgress is not {} progress || !HasComp<NarsiCultistComponent>(uid) ||
            !TryComp<MobStateComponent>(uid, out var mob) || mob.CurrentState != MobState.Alive)
            return;

        SetNewCultistLeader(uid, progress);
    }
}
