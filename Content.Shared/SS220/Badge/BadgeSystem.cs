// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Storage;

namespace Content.Shared.SS220.Badge;

public sealed partial class BadgeSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BadgeComponent, ExaminedEvent>(OnExamined);
    }

    public void SetBadgeData(EntityUid ent, string characterName, JobPrototype jobPrototype)
    {
        if (!_inventory.TryGetSlotEntity(ent, "back", out var backUid))
            return;

        if (!TryComp<StorageComponent>(backUid, out var storage))
            return;

        EntityUid? badgeUid = null;
        foreach (var contained in storage.Container.ContainedEntities)
        {
            if (HasComp<BadgeComponent>(contained))
            {
                badgeUid = contained;
                break;
            }
        }

        if (!TryComp<BadgeComponent>(badgeUid, out var badge))
            return;

        badge.FullName = characterName;
        badge.JobName = jobPrototype.LocalizedName;
        badge.SerialNumber = badgeUid.Value.Id.ToString("X6");
        Dirty(badgeUid.Value, badge);

        _metaData.SetEntityName(badgeUid.Value, $"{Loc.GetString(badge.Label)}, {characterName}");
    }

    private void OnExamined(Entity<BadgeComponent> ent, ref ExaminedEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.FullName) || string.IsNullOrWhiteSpace(ent.Comp.JobName))
            return;

        args.PushMarkup(
            $"[color={ent.Comp.Color}]" +
            $"{Loc.GetString(ent.Comp.Department)}\n" +
            $"{ent.Comp.FullName}\n" +
            $"{ent.Comp.JobName}\n" +
            $"№ NT-FNK-{ent.Comp.SerialNumber}" +
            "[/color]"
        );
    }
}
