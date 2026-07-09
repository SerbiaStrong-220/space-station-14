// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.SS220.Chemistry;

public sealed class RefillableSolutionRelaySystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RefillableSolutionComponent, SolutionContainerChangedEvent>(OnChanged);
    }

    private void OnChanged(Entity<RefillableSolutionComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, args.SolutionId, out var soln, out _))
            return;

        var ev = new RefillableSolutionChangedEvent(ent, args.SolutionId, soln.Value);
        RaiseLocalEvent(ref ev);
    }
}

[ByRefEvent]
public readonly record struct RefillableSolutionChangedEvent(
    Entity<RefillableSolutionComponent> Container,
    string SolutionId,
    Entity<SolutionComponent> Solution);
