// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Delivery;
using Content.Shared.EntityTable;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.SS220.EntityTable;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Delivery;

public sealed partial class DeliverySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private void FillContents(
        Entity<DeliveryComponent> ent,
        ProtoId<JobPrototype>? job,
        ProtoId<SpeciesPrototype>? species)
    {
        if (ent.Comp.Contents == null)
            return;

        Dictionary<string, object> data = [];
        if (job != null)
            data.Add(JobSelector.JobContextKey, job.Value);
        if (species != null)
            data.Add(SpeciesSelector.SpeciesContextKey, species.Value);

        var context = new EntityTableContext(data);

        var xform = Transform(ent);
        var container = _container.GetContainer(ent, ent.Comp.Container);
        var coordinates = new EntityCoordinates(ent, System.Numerics.Vector2.Zero);
        foreach (var prototype in _entityTable.GetSpawns(ent.Comp.Contents, ctx: context))
        {
            var item = Spawn(prototype, coordinates);
            if (_container.Insert(item, container, containerXform: xform))
                continue;

            Log.Error($"Failed to insert {ToPrettyString(item)} into delivery {ToPrettyString(ent)}.");
            _transform.AttachToGridOrMap(item);
            break;
        }
    }
}
