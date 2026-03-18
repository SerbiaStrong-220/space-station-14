using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Reaction;

namespace Content.Server.SS220.BeerUpdate.Fermentation;

public sealed class FermentationSolutionContainerSystem : EntitySystem
{
    [Dependency] private readonly ChemicalReactionSystem _reactions = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
}
