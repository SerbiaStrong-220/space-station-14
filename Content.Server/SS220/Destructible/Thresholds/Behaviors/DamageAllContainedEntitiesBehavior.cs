// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Destructible.Thresholds.Behaviors;

/// <summary>
/// Deals damage to all the entities in the container
/// </summary>
[DataDefinition]
public sealed partial class DamageAllContainedEntitiesBehavior : IThresholdBehavior
{
    // Specify damage amount and type to deal
    [DataField(required: true)]
    public DamageSpecifier Damage { get; set; } = new();

    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        if (!system.EntityManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
            return;

        var damageableSystem = system.EntityManager.System<DamageableSystem>();

        foreach (var container in containerManager.Containers.Values)
        {
            foreach (var entity in container.ContainedEntities)
            {
                damageableSystem.TryChangeDamage(entity, Damage, true, origin: owner);
            }
        }
    }
}
