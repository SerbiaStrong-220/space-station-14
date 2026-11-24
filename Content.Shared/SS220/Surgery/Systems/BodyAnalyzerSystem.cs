// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Surgery.Systems;

public sealed partial class BodyAnalyzerSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedSurgerySystem _surgery = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyAnalyzerComponent, AfterInteractEvent>(OnBodyAnalyzerAfterInteract);
        SubscribeLocalEvent<BodyAnalyzerComponent, BoundUIOpenedEvent>(OnBodyAnalyzerUiOpened);
        Subs.BuiEvents<OperatingComputerComponent>(BodyAnalyzerUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOperatingComputerUiOpened);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var computerQuery = GetEntityQuery<OperatingComputerComponent>();
        var analyzerQuery = EntityQueryEnumerator<BodyAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.NextUpdate == TimeSpan.Zero || _timing.CurTime - component.NextUpdate > component.UpdateInterval)
                component.NextUpdate = _timing.CurTime + component.UpdateInterval;
            else
                component.NextUpdate += component.UpdateInterval;

            if (computerQuery.TryComp(uid, out var computerComp))
            {
                if (!_userInterface.IsUiOpen(uid, BodyAnalyzerUiKey.Key))
                    continue;

                var tablePatient = FindBuckledPatientOnLinkedTable((uid, computerComp));
                UpdateAnalyzerTarget((uid, component), tablePatient);
                continue;
            }

            if (!_userInterface.IsUiOpen(uid, BodyAnalyzerUiKey.Key))
                continue;

            if (component.ScannedEntity is not { } patient)
                continue;

            if (!TryResolveTargetInRange((uid, component, transform), patient, out var validTarget))
            {
                UpdateAnalyzerTarget((uid, component), null);
                continue;
            }

            UpdateAnalyzerTarget((uid, component), validTarget);
        }
    }

    private void OnBodyAnalyzerUiOpened(Entity<BodyAnalyzerComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (_net.IsClient || HasComp<OperatingComputerComponent>(entity))
            return;

        if (entity.Comp.ScannedEntity is not { } patient)
            return;

        var validTarget = TryResolveTargetInRange((entity.Owner, entity.Comp, Transform(entity.Owner)), patient, out var resolved)
            ? resolved
            : null;
        UpdateAnalyzerTarget(entity, validTarget);
    }

    private bool TryResolveTargetInRange(
        Entity<BodyAnalyzerComponent, TransformComponent> analyzer,
        EntityUid patient,
        out EntityUid? target)
    {
        target = null;
        if (Deleted(patient))
            return false;

        var patientCoordinates = Transform(patient).Coordinates;
        if (analyzer.Comp1.MaxScanRange != null &&
            !_transformSystem.InRange(patientCoordinates, analyzer.Comp2.Coordinates, analyzer.Comp1.MaxScanRange.Value))
        {
            return false;
        }

        target = patient;
        return true;
    }

    private void OnOperatingComputerUiOpened(Entity<OperatingComputerComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<BodyAnalyzerComponent>(entity, out var analyzer))
            return;

        var patient = FindBuckledPatientOnLinkedTable(entity);
        UpdateAnalyzerTarget((entity.Owner, analyzer), patient);
    }

    private EntityUid? FindBuckledPatientOnLinkedTable(Entity<OperatingComputerComponent> computer)
    {
        if (computer.Comp.LinkedTable is not { } tableUid ||
            !TryComp<OperatingTableComponent>(tableUid, out var tableComp) ||
            !_surgery.IsOperatingLinkActive(computer, (tableUid, tableComp)) ||
            !TryComp<StrapComponent>(tableUid, out var strap))
        {
            return null;
        }

        foreach (var buckled in strap.BuckledEntities)
        {
            if (!Deleted(buckled))
                return buckled;
        }

        return null;
    }

    private void OnBodyAnalyzerAfterInteract(Entity<BodyAnalyzerComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null)
        {
            if (_net.IsServer)
                UpdateAnalyzerTarget(entity, null);

            return;
        }

        if (!_userInterface.HasUi(entity, BodyAnalyzerUiKey.Key))
        {
            Log.Debug($"Entity {ToPrettyString(entity)} has {nameof(BodyAnalyzerComponent)} but don't have its UI!");
            return;
        }

        // handled on the client too, so it doesn't predict another interaction with the target
        args.Handled = true;

        // ScannedEntity and the UI state are server-side only
        if (_net.IsClient)
            return;

        _userInterface.OpenUi(entity.Owner, BodyAnalyzerUiKey.Key, args.User);

        UpdateAnalyzerTarget(entity, args.Target.Value);
    }

    public bool IsComputerTableUnlinked(EntityUid computerUid)
    {
        if (!TryComp<OperatingComputerComponent>(computerUid, out var computer))
            return false;

        return computer.LinkedTable is not { } tableUid ||
               !TryComp<OperatingTableComponent>(tableUid, out var tableComp) ||
               !_surgery.IsOperatingLinkActive((computerUid, computer), (tableUid, tableComp));
    }

    private void UpdateAnalyzerTarget(Entity<BodyAnalyzerComponent> analyzer, EntityUid? target)
    {
        analyzer.Comp.ScannedEntity = target;
        var netTarget = GetNetEntity(target);
        var tableNotLinked = IsComputerTableUnlinked(analyzer.Owner);

        var state = new BodyAnalyzerTargetUpdate(netTarget, tableNotLinked);
        _userInterface.SetUiState(analyzer.Owner, BodyAnalyzerUiKey.Key, state);
    }
}
