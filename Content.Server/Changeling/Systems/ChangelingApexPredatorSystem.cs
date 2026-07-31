// SS220 changeling Apex tracker
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Alert;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Systems;

/// <summary>
/// Selects and tracks a living member of the crew for Apex Predator.
/// Target identity never leaves the server; only the resulting arrow angle is owner-networked.
/// </summary>
public sealed class ChangelingApexPredatorSystem : EntitySystem
{
    private const string BoundUserInterfaceName = "ChangelingApexTrackerBoundUserInterface";
    private static readonly ProtoId<AlertPrototype> TrackingAlert = "ChangelingApexTarget";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100);

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedIdCardSystem _idCards = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingApexPredatorActionEvent>(OnApexPredator);
        SubscribeLocalEvent<ChangelingApexTrackerComponent, ChangelingApexTargetSelectedMessage>(OnTargetSelected);
        SubscribeLocalEvent<ChangelingApexTrackerComponent, ChangelingEvolutionResetEvent>(OnEvolutionReset);
        SubscribeLocalEvent<ChangelingApexTrackerComponent, BoundUserInterfaceMessageAttempt>(OnTrackerUiMessageAttempt);
        SubscribeLocalEvent<ChangelingApexTrackerComponent, BoundUIClosedEvent>(OnTrackerUiClosed);
        SubscribeLocalEvent<ChangelingApexTrackerComponent, ComponentShutdown>(OnTrackerShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ChangelingApexTrackerComponent>();
        while (query.MoveNext(out var uid, out var tracker))
        {
            if (!HasComp<ChangelingResourceComponent>(uid))
            {
                RemCompDeferred<ChangelingApexTrackerComponent>(uid);
                continue;
            }

            if (tracker.Target is not { } target || now < tracker.NextUpdate)
                continue;

            tracker.NextUpdate += UpdateInterval;
            if (!IsTrackable(uid, target))
            {
                ClearTarget((uid, tracker));
                continue;
            }

            var ownerTransform = Transform(uid);
            var targetTransform = Transform(target);
            var direction = _transform.GetWorldPosition(targetTransform) -
                            _transform.GetWorldPosition(ownerTransform);
            var angle = direction.LengthSquared() > 0.01f
                ? direction.ToWorldAngle()
                : Angle.Zero;

            if (tracker.ArrowAngle.EqualsApprox(angle, 0.01))
                continue;

            tracker.ArrowAngle = angle;
            Dirty(uid, tracker);
        }
    }

    private void OnApexPredator(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingApexPredatorActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;
        var tracker = EnsureComp<ChangelingApexTrackerComponent>(ent.Owner);
        var userInterface = EnsureComp<UserInterfaceComponent>(ent.Owner);
        _ui.SetUi(
            (ent.Owner, userInterface),
            ChangelingApexTrackerUiKey.Key,
            new InterfaceData(BoundUserInterfaceName));

        var targets = BuildTargetList((ent.Owner, tracker));
        _ui.SetUiState(
            (ent.Owner, userInterface),
            ChangelingApexTrackerUiKey.Key,
            new ChangelingApexTrackerUiState(targets));

        if (targets.Count == 0)
            _popup.PopupEntity(Loc.GetString("changeling-apex-no-targets"), ent.Owner, ent.Owner);

        if (!_ui.IsUiOpen(
                (ent.Owner, userInterface),
                ChangelingApexTrackerUiKey.Key,
                args.Performer))
        {
            _ui.OpenUi(
                (ent.Owner, userInterface),
                ChangelingApexTrackerUiKey.Key,
                args.Performer);
        }

        tracker.NextUpdate = _timing.CurTime;
    }

    private void OnTargetSelected(
        Entity<ChangelingApexTrackerComponent> ent,
        ref ChangelingApexTargetSelectedMessage args)
    {
        if (args.Actor != ent.Owner || !HasComp<ChangelingResourceComponent>(ent.Owner))
            return;

        if (!ent.Comp.TargetSelectionTokens.TryGetValue(args.SelectionToken, out var target) ||
            !IsEligibleCrew(ent.Owner, target))
        {
            // Invalid tokens are untrusted network input. Fail closed without rebuilding and sorting the entire
            // crew roster, otherwise the owner could turn forged BUI messages into an allocation/CPU amplifier.
            ent.Comp.TargetSelectionTokens.Clear();
            _popup.PopupEntity(Loc.GetString("changeling-apex-invalid-target"), ent.Owner, ent.Owner);
            _ui.CloseUi(ent.Owner, ChangelingApexTrackerUiKey.Key, ent.Owner);
            return;
        }

        ent.Comp.TargetSelectionTokens.Clear();
        ent.Comp.Target = target;
        ent.Comp.NextUpdate = _timing.CurTime;
        _alerts.ShowAlert(ent.Owner, TrackingAlert);
        _popup.PopupEntity(
            Loc.GetString(
                "changeling-apex-target-selected",
                ("target", Identity.Name(target, EntityManager, ent.Owner))),
            ent.Owner,
            ent.Owner);
        _ui.CloseUi(ent.Owner, ChangelingApexTrackerUiKey.Key, ent.Owner);
    }

    private void OnEvolutionReset(
        Entity<ChangelingApexTrackerComponent> ent,
        ref ChangelingEvolutionResetEvent args)
    {
        RemComp<ChangelingApexTrackerComponent>(ent.Owner);
    }

    private void OnTrackerUiMessageAttempt(
        Entity<ChangelingApexTrackerComponent> ent,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.UiKey.Equals(ChangelingApexTrackerUiKey.Key) && args.Actor != ent.Owner)
            args.Cancel();
    }

    private void OnTrackerUiClosed(
        Entity<ChangelingApexTrackerComponent> ent,
        ref BoundUIClosedEvent args)
    {
        if (args.UiKey.Equals(ChangelingApexTrackerUiKey.Key))
            ent.Comp.TargetSelectionTokens.Clear();
    }

    private void OnTrackerShutdown(
        Entity<ChangelingApexTrackerComponent> ent,
        ref ComponentShutdown args)
    {
        ent.Comp.TargetSelectionTokens.Clear();
        _alerts.ClearAlert(ent.Owner, TrackingAlert);
        _ui.CloseUi(ent.Owner, ChangelingApexTrackerUiKey.Key);
    }

    private List<ChangelingApexTargetEntry> BuildTargetList(Entity<ChangelingApexTrackerComponent> ent)
    {
        ent.Comp.TargetSelectionTokens.Clear();
        var targets = new List<ChangelingApexTargetEntry>();
        var query = EntityQueryEnumerator<HumanoidProfileComponent, MindContainerComponent, MobStateComponent>();
        while (query.MoveNext(out var candidate, out _, out _, out _))
        {
            if (!IsEligibleCrew(ent.Owner, candidate))
                continue;

            ProtoId<JobIconPrototype>? jobIcon = null;
            if (_idCards.TryFindIdCard(candidate, out var idCard))
                jobIcon = idCard.Comp.JobIcon;

            var token = ent.Comp.NextSelectionToken++;
            if (token == 0)
            {
                token = ent.Comp.NextSelectionToken++;
            }

            ent.Comp.TargetSelectionTokens.Add(token, candidate);
            targets.Add(new ChangelingApexTargetEntry(
                token,
                Identity.Name(candidate, EntityManager, ent.Owner),
                jobIcon));
        }

        targets.Sort(static (left, right) =>
            StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
        return targets;
    }

    private bool IsEligibleCrew(EntityUid owner, EntityUid target)
    {
        if (!IsTrackable(owner, target) ||
            !TryComp<MindContainerComponent>(target, out var mindContainer) ||
            mindContainer.Mind is not { } mind ||
            !_roles.MindHasRole<JobRoleComponent>(mind, out _))
        {
            return false;
        }

        return _station.GetOwningStation(owner) is { } station &&
               _station.GetOwningStation(target) == station;
    }

    private bool IsTrackable(EntityUid owner, EntityUid target)
    {
        if (owner == target ||
            TerminatingOrDeleted(owner) ||
            TerminatingOrDeleted(target) ||
            !HasComp<HumanoidProfileComponent>(target) ||
            !TryComp<MobStateComponent>(target, out var mobState) ||
            !_mobState.IsAlive(target, mobState) ||
            !TryComp<MindContainerComponent>(target, out var mindContainer) ||
            !mindContainer.HasMind ||
            mindContainer.Mind is not { } mind ||
            !_roles.MindHasRole<JobRoleComponent>(mind, out _))
        {
            return false;
        }

        return Transform(owner).MapID == Transform(target).MapID;
    }

    private void ClearTarget(Entity<ChangelingApexTrackerComponent> ent)
    {
        ent.Comp.Target = null;
        _alerts.ClearAlert(ent.Owner, TrackingAlert);
        _popup.PopupEntity(Loc.GetString("changeling-apex-target-lost"), ent.Owner, ent.Owner);
    }

}
