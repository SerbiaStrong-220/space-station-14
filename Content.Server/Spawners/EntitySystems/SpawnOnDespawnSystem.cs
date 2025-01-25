using Content.Server.Spawners.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnOnDespawnSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnDespawnComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, SpawnOnDespawnComponent comp, ref TimedDespawnEvent args)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return;

        // SS220 Add inherit rotation begin
        //Spawn(comp.Prototype, xform.Coordinates);

        if (comp.InheritRotation)
        {
            var mapCords = _xform.ToMapCoordinates(GetNetCoordinates(xform.Coordinates));
            Spawn(comp.Prototype, mapCords, rotation: xform.LocalRotation);
        }
        else
            Spawn(comp.Prototype, xform.Coordinates);
        // SS220 Add inherit rotation end
    }

    public void SetPrototype(Entity<SpawnOnDespawnComponent> entity, EntProtoId prototype)
    {
        entity.Comp.Prototype = prototype;
    }
}
