// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Surgery.Systems;

public sealed partial class BodyAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private readonly HashSet<Entity<OperatingTableComponent>> _tableBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyAnalyzerComponent, AfterInteractEvent>(OnBodyAnalyzerAfterInteract);
        Subs.BuiEvents<OperatingComputerComponent>(BodyAnalyzerUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOperatingComputerUiOpened);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var analyzerQuery = EntityQueryEnumerator<BodyAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.NextUpdate == TimeSpan.Zero || _timing.CurTime - component.NextUpdate > component.UpdateInterval)
                component.NextUpdate = _timing.CurTime + component.UpdateInterval;
            else
                component.NextUpdate += component.UpdateInterval;

            if (TryComp<OperatingComputerComponent>(uid, out var computerComp))
            {
                if (!_userInterface.IsUiOpen(uid, BodyAnalyzerUiKey.Key))
                    continue;

                var tablePatient = FindBuckledPatientOnLinkedTable((uid, computerComp), transform);
                UpdateAnalyzerTarget((uid, component), tablePatient);
                continue;
            }

            if (component.ScannedEntity is not { } patient)
                continue;

            if (Deleted(patient))
            {
                UpdateAnalyzerTarget((uid, component), null);
                continue;
            }

            var patientCoordinates = Transform(patient).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                UpdateAnalyzerTarget((uid, component), null);
                continue;
            }

            UpdateAnalyzerTarget((uid, component), patient);
        }
    }

    private void OnOperatingComputerUiOpened(Entity<OperatingComputerComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (!TryComp<BodyAnalyzerComponent>(entity, out var analyzer))
            return;

        var patient = FindBuckledPatientOnLinkedTable(entity, Transform(entity));
        UpdateAnalyzerTarget((entity.Owner, analyzer), patient);
    }

    private EntityUid? FindBuckledPatientOnLinkedTable(Entity<OperatingComputerComponent> computer, TransformComponent xform)
    {
        _tableBuffer.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, computer.Comp.TableSearchRange, _tableBuffer);

        foreach (var table in _tableBuffer)
        {
            if (!TryComp<StrapComponent>(table, out var strap))
                continue;

            foreach (var buckled in strap.BuckledEntities)
            {
                if (!Deleted(buckled))
                    return buckled;
            }
        }

        return null;
    }

    private void OnBodyAnalyzerAfterInteract(Entity<BodyAnalyzerComponent> entity, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (args.Target == null)
        {
            UpdateAnalyzerTarget(entity, args.Target);
            return;
        }

        if (!_userInterface.HasUi(entity, BodyAnalyzerUiKey.Key))
        {
            Log.Debug($"Entity {ToPrettyString(entity)} has {nameof(BodyAnalyzerComponent)} but don't have its UI!");
            return;
        }

        _userInterface.OpenUi(entity.Owner, BodyAnalyzerUiKey.Key, args.User);

        UpdateAnalyzerTarget(entity, args.Target.Value);
    }

    private void UpdateAnalyzerTarget(Entity<BodyAnalyzerComponent> analyzer, EntityUid? target)
    {
        analyzer.Comp.ScannedEntity = target;
        var netTarget = GetNetEntity(target);

        var state = new BodyAnalyzerTargetUpdate(netTarget);
        _userInterface.SetUiState(analyzer.Owner, BodyAnalyzerUiKey.Key, state);
    }
}
