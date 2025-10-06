using System.Linq;
using Content.Server.Disposal.Tube;
using Content.Shared.SS220.Pipes.DisposalFilter;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Pipes.DisposalFilter;

public sealed class DisposalFilterSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DisposalFilterComponent, GetDisposalsNextDirectionEvent>(OnGetDirection, after: [typeof(DisposalTubeSystem)]);
        SubscribeLocalEvent<DisposalFilterComponent, DisposalFilterBoundMessage>(OnUiMessage);
        SubscribeLocalEvent<DisposalFilterComponent, BoundUIOpenedEvent>(OnUiOpened);
    }

    private void OnGetDirection(Entity<DisposalFilterComponent> ent, ref GetDisposalsNextDirectionEvent args)
    {
        var item = new EntityUid();
        foreach (var entity in args.Holder.Container.ContainedEntities)
        {
            item = entity;
            break;
        }

        if (item == default)
            return;

        foreach (var dir in ent.Comp.FilterByDir)
        {
            if (!dir.Matches(item, EntityManager))
                continue;

            args.Next = dir.OutputDir;
            return;
        }

        args.Next = ent.Comp.BaseDirection ?? Transform(ent).LocalRotation.GetDir();
    }

    private void OnUiMessage(Entity<DisposalFilterComponent> ent, ref DisposalFilterBoundMessage args)
    {
        ent.Comp.FilterByDir.Clear();
        foreach (var dir in args.DirByRules)
        {
            ent.Comp.FilterByDir.Add(dir);
        }

        ent.Comp.BaseDirection = args.BaseDir;

        var state = new DisposalFilterBoundState(ent.Comp.FilterByDir, ent.Comp.BaseDirection.Value);
        _ui.SetUiState(ent.Owner, DisposalFilterUiKey.Key, state);
    }

    private void OnUiOpened(Entity<DisposalFilterComponent> ent, ref BoundUIOpenedEvent args)
    {
        List<Angle> degrees = new();
        if (TryComp<DisposalJunctionComponent>(ent, out var junction))
            degrees = junction.Degrees;

        if (TryComp<DisposalRouterComponent>(ent, out var router))
            degrees = router.Degrees;

        var ev = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(ent, ref ev);

        foreach (var angle in degrees)
        {
            var dir = (angle + Transform(ent).LocalRotation).GetDir();
            if (!ev.Connectable.Contains(dir))
                continue;

            var opposite = Transform(ent).LocalRotation.GetDir().GetOpposite();
            if (dir == opposite)
                continue;

            if (ent.Comp.FilterByDir.All(rule => rule.OutputDir != dir))
            {
                ent.Comp.FilterByDir.Add(new FilterRule
                {
                    OutputDir = dir,
                });
            }
        }

        ent.Comp.BaseDirection ??= Transform(ent).LocalRotation.GetDir();

        var state = new DisposalFilterBoundState(ent.Comp.FilterByDir, ent.Comp.BaseDirection.Value);
        _ui.SetUiState(ent.Owner, DisposalFilterUiKey.Key, state);
    }
}
