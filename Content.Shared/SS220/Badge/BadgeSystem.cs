// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Roles;

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
        if (!_inventory.TryGetSlotEntity(ent, "neck", out var badgeUid))
            return;

        // нужно сделать, чтобы работало в рюкзаке. Пока не получилось, увырге

        if (!TryComp<BadgeComponent>(badgeUid, out var badge))
            return;

        badge.FullName = characterName;
        badge.JobName = jobPrototype.LocalizedName;
        badge.SerialNumber = badgeUid.Value.Id.ToString("X8");
        Dirty(badgeUid.Value, badge);

        _metaData.SetEntityName(badgeUid.Value, $"{Loc.GetString(badge.Label)}, {characterName}");
    }

    private void OnExamined(Entity<BadgeComponent> ent, ref ExaminedEvent args)
    { // Нужно синхронизировать это между клиентом и сервером?, НО КАК БЛЯТЬ, ЧТО ОНО НЕ ПОДАДЁТСЯ
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
