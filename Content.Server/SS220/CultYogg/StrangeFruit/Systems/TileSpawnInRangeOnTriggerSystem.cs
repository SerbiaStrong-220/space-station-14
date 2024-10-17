using Content.Server.Explosion.EntitySystems;
using Content.Server.SS220.CultYogg.StrangeFruit.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.StrangeFruit.Systems;

public sealed class TileSpawnInRangeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileSpawnInRangeOnTriggerComponent, TriggerEvent>(TileSpawnInRangeTrigger);
    }

    private void TileSpawnInRangeTrigger(Entity<TileSpawnInRangeOnTriggerComponent> entity, ref TriggerEvent args)
    {
        if (!(entity.Comp.Range > 0))
        {
            Log.Error("Range must be positive");
            return;
        }

        if(!_prototypeManager.TryIndex(entity.Comp.KudzuProtoId, out var prototype, logError: true))
            return;

        var xform = Transform(entity);
        var mapcord = _transformSystem.GetMapCoordinates(entity, xform);
        for (var x = (int)mapcord.X - entity.Comp.Range ; x <= (int)mapcord.X + entity.Comp.Range; x++)
        {
            for (var y = (int)mapcord.Y - entity.Comp.Range ; y <= (int)mapcord.Y + entity.Comp.Range; y++)
            {
                var nmap = new MapCoordinates(x, y, mapcord.MapId);
                Spawn(entity.Comp.KudzuProtoId, nmap);
            }
        }
        args.Handled = true;
    }
}
