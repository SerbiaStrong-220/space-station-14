// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Mind.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Containers;

public sealed class ContainerMobEjectSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public void DropAllMindsFromEntity(EntityUid uid)
    {
        if (TryComp(uid, out ContainerManagerComponent? contMan))
        {
            foreach (var container in contMan.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    if (HasComp<MindContainerComponent>(ent))
                    {
                        _container.TryRemoveFromContainer(ent);
                        _transform.SetWorldPosition(ent, _transform.GetWorldPosition(uid));
                    }
                }
            }
        }

        //hands
    }
}
