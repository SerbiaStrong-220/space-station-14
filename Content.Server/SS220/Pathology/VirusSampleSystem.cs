// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class VirusSampleSystem : EntitySystem
{
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusSampleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<VirusSampleComponent> ent, ref MapInitEvent args)
    {
        if (_pathology.BuildVirus(ent.Comp.Virus) is not { } virus)
            return;

        if (!_solutionContainer.EnsureSolutionEntity((ent.Owner, null), ent.Comp.Solution, out var soln, ent.Comp.Amount))
            return;

        var virusData = new VirusData { Viruses = new List<VirusInstance> { virus } };
        _solutionContainer.TryAddReagent(soln.Value, ent.Comp.Carrier, ent.Comp.Amount, out _, data: new List<ReagentData> { virusData });
    }
}
