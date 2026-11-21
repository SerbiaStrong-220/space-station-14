using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.LiquidSplatters;

public abstract class SharedLiquidSplattersSystem : EntitySystem
{
    [Dependency] protected SharedSolutionContainerSystem _solution = default!;
    [Dependency] protected IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LiquidSplattersComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<LiquidSplattersComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if(!_solution.TryGetSolution(ent.Owner, ent.Comp.ContainerName, out _, out var solution) ||
           solution.Volume <= 0)
            return;

        if (solution.GetPrimaryReagentId() is not { } primaryId
            || !_proto.TryIndex<ReagentPrototype>(primaryId.Prototype, out var primary))
            return;

        var name = primary.Recognizable ? primary.LocalizedName : primary.LocalizedPhysicalDescription;
        using (args.PushGroup(nameof(LiquidSplattersComponent)))
        {
            args.PushMarkup(Loc.GetString("liquid-splatters-examine",
                ("level", GetStainLevel(ent.Comp, solution)),
                ("color", solution.GetColor(_proto).ToHexNoAlpha()),
                ("reagent", name)));
        }
    }

    private static string GetStainLevel(LiquidSplattersComponent comp, Solution solution)
    {
        var fill = solution.Volume.Float() / comp.MaxVolume;
        return fill switch
        {
            < 0.33f => "light",
            < 0.66f => "medium",
            _ => "heavy",
        };
    }
}
