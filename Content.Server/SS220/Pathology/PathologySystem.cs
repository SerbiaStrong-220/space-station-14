// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem : SharedPathologySystem
{
    protected override void ApplyPathologyContext(Entity<PathologyHolderComponent> entity, IPathologyContext? context)
    {
        base.ApplyPathologyContext(entity, context);

        switch (context)
        {
            case EntityProvidedPathologyContext entityProvided:
                HandleEntityProvidedContext(entity, entityProvided);
                break;

            default:
                break;
        }
    }

    private void HandleEntityProvidedContext(Entity<PathologyHolderComponent> entity, EntityProvidedPathologyContext context)
    {
        if (!TrySpawnNextTo(context.ProtoId, entity.Owner, out var spawnedUid))
            return;

        var forensicsComponent = EnsureComp<ForensicsComponent>(spawnedUid.Value);

        forensicsComponent.Fingerprints.UnionWith(context.Fingerprints);
        forensicsComponent.DNAs.UnionWith(context.DNAs);
    }
}
