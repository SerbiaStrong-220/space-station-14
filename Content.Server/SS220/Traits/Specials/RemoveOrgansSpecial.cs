// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Body;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Traits.Specials;

// Detaches and deletes organs with the given categories.
[UsedImplicitly]
public sealed partial class RemoveOrgansSpecial : JobSpecial
{
    [DataField(required: true)]
    public HashSet<ProtoId<OrganCategoryPrototype>> Categories { get; private set; } = [];

    public override void AfterEquip(EntityUid uid)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryGetComponent<BodyComponent>(uid, out var body) || body.Organs == null)
            return;

        var transformSystem = entMan.System<SharedTransformSystem>();

        var toRemove = new List<EntityUid>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!entMan.TryGetComponent<OrganComponent>(organ, out var organComp)
                || organComp.Category is not { } category
                || !Categories.Contains(category))
            {
                continue;
            }

            toRemove.Add(organ);
        }

        foreach (var organ in toRemove)
        {
            transformSystem.AttachToGridOrMap(organ);
            entMan.QueueDeleteEntity(organ);
        }
    }
}

