using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using ProtogonistRole = Content.Server.Roles.ProtogonistRole;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ProtogonistRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mindSystem = entityManager.System<MindSystem>();
            return mindSystem.HasRole<ProtogonistRole>(mind);
        }
    }
}
