using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry;
using Robust.Shared.GameStates;
using Content.Shared.Chemistry.Components;

namespace Content.Server.SS220.Autoinjector
{
    public sealed partial class AutoinjectorSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<AutoinjectorComponent, AfterHypoEvent>(OnAfterHypo);
        }

        private void OnAfterHypo(EntityUid uid, AutoinjectorComponent component, AfterHypoEvent args)
        {
            if (!TryComp<HyposprayComponent>(uid, out var hypoComp))
                return;
            if (!_solutionContainerSystem.TryGetSolution(uid, hypoComp.SolutionName, out _, out var sol))
                return;
            RemComp<RefillableSolutionComponent>(uid);

        }
    }
}

