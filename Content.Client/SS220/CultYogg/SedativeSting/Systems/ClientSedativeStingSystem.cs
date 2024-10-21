using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.SS220.CultYogg.SedativeSting.Components;

namespace Content.Client.SS220.CultYogg.SedativeSting.Systems;

public sealed class ClientSedativeStingSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<SedativeStingComponent>(ent => new SedativeStingStatusControl(ent, _solutionContainers));
    }
}
