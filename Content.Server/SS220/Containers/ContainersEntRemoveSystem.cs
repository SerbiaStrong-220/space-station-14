// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Containers;

public sealed class ContainerEntRemoveSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <summary>
    ///     Removes all entites with specified component from entity containers, including hands.
    /// </summary>
    public void DropAllTargetsFromEntityContainers<T>(EntityUid uid) where T : IComponent
    {
        void EjectRecursive(EntityUid uid)
        {
            if (!TryComp<ContainerManagerComponent>(uid, out var contManager))
                return;

            foreach (var container in contManager.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities.ToList())
                {
                    if (HasComp<T>(ent))
                    {
                        _container.TryRemoveFromContainer(ent);
                        _transform.SetWorldPosition(ent, _transform.GetWorldPosition(uid));
                    }

                    EjectRecursive(ent);
                }
            }
        }

        EjectRecursive(uid);

        if (!TryComp<HandsComponent>(uid, out var hands))
            return;
        if (hands.ActiveHand == null)
            return;
        foreach (var held in _hands.EnumerateHeld(uid, hands))
        {
            if (HasComp<T>(held))
            {
                _hands.TryDrop(uid, handsComp: hands);
            }
        }
    }
}
