using System.Linq;
using Content.Server.Disposal.Tube;
using Content.Shared.Rotatable;
using Content.Shared.SS220.Pipes.DisposalFilter;
using Robust.Server.GameObjects;
// ReSharper disable InvertIf

namespace Content.Server.SS220.Pipes.DisposalFilter;

public sealed class DisposalFilterSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DisposalFilterComponent, GetDisposalsNextDirectionEvent>(OnGetDirection, after: [typeof(DisposalTubeSystem)]);
        SubscribeLocalEvent<DisposalFilterComponent, DisposalFilterBoundMessage>(OnUiMessage);
        SubscribeLocalEvent<DisposalFilterComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DisposalFilterComponent, RotateEvent>(OnRotate);
    }

    private void OnGetDirection(Entity<DisposalFilterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        if (args.Holder.Container.ContainedEntities.Count == 0)
            return;

        var item = args.Holder.Container.ContainedEntities[0];

        foreach (var dir in ent.Comp.FilterByDir)
        {
            var matches = dir.Matches(item, EntityManager);

            if (matches)
            {
                args.Next = dir.OutputDir;
                return;
            }
        }

        args.Next = ent.Comp.BaseDirection ?? Transform(ent).LocalRotation.GetDir();
    }

    private void OnUiMessage(Entity<DisposalFilterComponent> ent, ref DisposalFilterBoundMessage args)
    {
        ent.Comp.FilterByDir.Clear();
        ent.Comp.FilterByDir = args.DirByRules;

        ent.Comp.BaseDirection = args.BaseDir;

        var state = new DisposalFilterBoundState(ent.Comp.FilterByDir, ent.Comp.BaseDirection.Value);
        _ui.SetUiState(ent.Owner, DisposalFilterUiKey.Key, state);
    }

    private void OnUiOpened(Entity<DisposalFilterComponent> ent, ref BoundUIOpenedEvent args)
    {
        HandleBoundUI(ent);
    }

    private void OnRotate(Entity<DisposalFilterComponent> ent, ref RotateEvent args)
    {
        HandleBoundUI(ent);
    }

    private void HandleBoundUI(Entity<DisposalFilterComponent> ent)
    {
        List<Angle> degrees = new();
        if (TryComp<DisposalJunctionComponent>(ent, out var junction))
            degrees = junction.Degrees;

        if (TryComp<DisposalRouterComponent>(ent, out var router))
            degrees = router.Degrees;

        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(ent, ref ev);

        var localRot = Transform(ent).LocalRotation;
        var localDir = localRot.GetDir();
        var inputDir = localDir.GetOpposite();

        var baseDir = ent.Comp.BaseDirection ?? localDir;
        var existingFilters = ent.Comp.FilterByDir.ToDictionary(f => f.OutputDir, f => f);
        var newFilters = new List<DisposalFilterRule>();

        foreach (var angle in degrees)
        {
            var dir = (angle + localRot).GetDir();
            if (!ev.Connectable.Contains(dir))
                continue;

            if (dir == inputDir)
                continue;

            newFilters.Add(existingFilters.TryGetValue(dir, out var existing)
                ? existing
                : new DisposalFilterRule { OutputDir = dir });
        }

        ent.Comp.FilterByDir = newFilters;
        ent.Comp.BaseDirection = baseDir;

        var state = new DisposalFilterBoundState(ent.Comp.FilterByDir, ent.Comp.BaseDirection.Value);
        _ui.SetUiState(ent.Owner, DisposalFilterUiKey.Key, state);
    }
}
