// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Trigger;
using Robust.Shared.Map;

namespace Content.Server.SS220.CultYogg.StrangeFruit;

public sealed class TileSpawnInRangeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileSpawnInRangeOnTriggerComponent, TriggerEvent>(TileSpawnInRangeTrigger);
    }

    private void TileSpawnInRangeTrigger(Entity<TileSpawnInRangeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if ( ent.Comp.Range < 0)
        {
            Log.Error("Range must be positive");
            return;
        }

        var xform = Transform(ent);
        var mapcord = _transformSystem.GetMapCoordinates(ent, xform);
        for (var x = (int)mapcord.X - ent.Comp.Range; x <= (int)mapcord.X + ent.Comp.Range; x++)
        {
            for (var y = (int)mapcord.Y - ent.Comp.Range; y <= (int)mapcord.Y + ent.Comp.Range; y++)
            {
                var nmap = new MapCoordinates(x, y, mapcord.MapId);
                Spawn(ent.Comp.ProtoId, nmap);
            }
        }
        args.Handled = true;
    }
}
