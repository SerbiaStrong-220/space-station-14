// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport.Components;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed class SpawnBeforeTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnBeforeTeleportComponent, BeforeTeleportTargetEvent>(OnBeforeTeleport);
    }

    private void OnBeforeTeleport(Entity<SpawnBeforeTeleportComponent> ent, ref BeforeTeleportTargetEvent args)
    {
        var position = _transform.GetMapCoordinates(ent);
        EntityManager.PredictedSpawn(ent.Comp.SpawnedEnt, position);
    }
}
