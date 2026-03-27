using Content.Server.Storage.EntitySystems;
using Content.Shared.SS220.Contractor;

namespace Content.Server.SS220.Contractor;

public sealed class ContractorTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContractorTargetComponent, TeleportTargetToStationEvent>(OnTeleportTargetToStation);
    }

    private void OnTeleportTargetToStation(Entity<ContractorTargetComponent> ent, ref TeleportTargetToStationEvent args)
    {
        _transform.SetCoordinates(ent.Owner, ent.Comp.PortalPosition);
        var closetEntity = SpawnAtPosition("ClosetSteelBase", ent.Comp.PortalPosition);

        _entityStorage.Insert(ent.Owner, closetEntity);
        _entityStorage.OpenStorage(closetEntity);
        ent.Comp.EnteredPortalTime = TimeSpan.Zero;
    }
}
