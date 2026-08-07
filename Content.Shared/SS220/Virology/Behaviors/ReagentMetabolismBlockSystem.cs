// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.SS220.Virology.Behaviors;

public sealed class ReagentMetabolismBlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentMetabolismBlockComponent, MetabolismExclusionEvent>(OnMetabolismExclusion);
    }

    private void OnMetabolismExclusion(Entity<ReagentMetabolismBlockComponent> ent, ref MetabolismExclusionEvent args)
    {
        foreach (var reagent in ent.Comp.Reagents)
            args.Reagents.Add(new ReagentId(reagent, null));
    }
}
