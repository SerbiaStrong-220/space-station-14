// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using Content.Shared.Interaction;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Surgery.Systems;

public sealed class BodyAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyAnalyzerComponent, AfterInteractEvent>(OnBodyAnalyzerAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var analyzerQuery = EntityQueryEnumerator<BodyAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not { } patient)
                continue;

            if (Deleted(patient))
            {
                UpdateAnalyzerTarget((uid, component), null);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            var patientCoordinates = Transform(patient).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                UpdateAnalyzerTarget((uid, component), null);
                continue;
            }

            UpdateAnalyzerTarget((uid, component), patient);
        }
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
