using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Fluids.Components;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SpillBehavior : IThresholdBehavior
    {
        [DataField]
        public string? Solution;

        /// <summary>
        /// If there is a SpillableComponent on EntityUidowner use it to create a puddle/smear.
        /// Or whatever solution is specified in the behavior itself.
        /// If none are available do nothing.
        /// </summary>
        /// <param name="owner">Entity on which behavior is executed</param>
        /// <param name="system">system calling the behavior</param>
        /// <param name="cause"></param>
        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            var solutionContainerSystem = system.EntityManager.System<SharedSolutionContainerSystem>();
            var spillableSystem = system.EntityManager.System<PuddleSystem>();

            var coordinates = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;

            //ss220 fix dupe puddles start (delete after merge https://github.com/space-wizards/space-station-14/pull/33231)
            if (Solution == null)
                return;

            if (system.EntityManager.TryGetComponent(owner, out SpillableComponent? spill) &&
                spill.SolutionName == Solution)
            {
                return;
            }

            if (solutionContainerSystem.TryGetSolution(owner, Solution, out _, out var behaviorSolution))
            {
                spillableSystem.TrySplashSpillAt(owner, coordinates, behaviorSolution, out _, user: cause);
            }
            //ss220 fix dupe puddles end
        }
    }
}
