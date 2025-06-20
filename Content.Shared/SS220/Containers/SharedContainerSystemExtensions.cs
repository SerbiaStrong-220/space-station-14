// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind.Components;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Containers;

public sealed class SharedContainerSystemExtensions : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    ///     Removes all entites with specified component from entity containers, including hands.
    /// </summary>
    public void RemoveEntitiesFromAllContainers<T>(EntityUid owner, List<string>? blacklistedIds = null, bool recursive = true) where T : IComponent
    {
        void EjectRecursive(EntityUid uid)
        {
            if (!TryComp<ContainerManagerComponent>(uid, out var contManager))
                return;

            foreach (var container in contManager.Containers.Values)
            {
                if (blacklistedIds != null && blacklistedIds.Contains(container.ID))
                    continue;

                foreach (var ent in container.ContainedEntities.ToList())
                {
                    if (HasComp<T>(ent))
                    {
                        _container.TryRemoveFromContainer(ent);
                        _transform.SetWorldPosition(ent, _transform.GetWorldPosition(uid));
                    }

                    if (recursive)
                        EjectRecursive(ent);
                }
            }
        }

        EjectRecursive(owner);

        _hands.DropEntitesFromHands<MindContainerComponent>(owner);
    }
}
