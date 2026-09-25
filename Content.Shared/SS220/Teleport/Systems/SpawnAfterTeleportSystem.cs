// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport.Components;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class SpawnAfterTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnAfterTeleportComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<SpawnAfterTeleportComponent> ent, ref TargetTeleportedEvent args)
    {
        var arrivalCoordinates = _transform.GetMapCoordinates(args.Target);
        EntityManager.PredictedSpawn(ent.Comp.SpawnPrototype, arrivalCoordinates);
    }
}
