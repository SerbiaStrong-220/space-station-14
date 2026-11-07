// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Body;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Traits.Specials;

// Deletes the organs of the selected variant.
[UsedImplicitly]
public sealed partial class RemoveOrgansSpecial : JobSpecial, ITraitOptionSpecial
{
    [DataField(required: true)]
    public Dictionary<string, HashSet<ProtoId<OrganCategoryPrototype>>> VariantCategories { get; private set; } = [];

    [DataField(required: true)]
    public string DefaultVariant { get; private set; } = string.Empty;

    public override void AfterEquip(EntityUid uid)
    {
        AfterEquip(uid, null);
    }

    public void AfterEquip(EntityUid uid, string? option)
    {
        var variant = option ?? DefaultVariant;
        var categories = VariantCategories[variant];

        var entMan = IoCManager.Resolve<IEntityManager>();

        if (!entMan.TryGetComponent<BodyComponent>(uid, out var body) || body.Organs == null)
            return;

        var transformSystem = entMan.System<SharedTransformSystem>();

        var toRemove = new List<EntityUid>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!entMan.TryGetComponent<OrganComponent>(organ, out var organComp)
                || organComp.Category is not { } category
                || !categories.Contains(category))
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

