// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Badge;

public sealed partial class BadgeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<BadgeComponent, ExaminedEvent>(OnExamined);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId is not { } jobId || !_prototype.TryIndex<JobPrototype>(jobId, out var job))
            return;

        if (FindBadge(args.Mob) is not { } badge)
            return;

        var name = Name(args.Mob);
        badge.Comp.FullName = name;
        badge.Comp.JobName = job.LocalizedName;
        badge.Comp.SerialNumber = badge.Owner.Id.ToString("X6");
        Dirty(badge);

        _metaData.SetEntityName(badge, $"{Loc.GetString(badge.Comp.Label)}, {name}");
    }

    private Entity<BadgeComponent>? FindBadge(EntityUid mob)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(mob, out var slots))
            return null;

        while (slots.NextItem(out var item))
        {
            if (TryComp<BadgeComponent>(item, out var badge))
                return (item, badge);

            if (!TryComp<StorageComponent>(item, out var storage))
                continue;

            foreach (var contained in storage.Container.ContainedEntities)
            {
                if (TryComp(contained, out badge))
                    return (contained, badge);
            }
        }

        return null;
    }

    private void OnExamined(Entity<BadgeComponent> ent, ref ExaminedEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.FullName) || string.IsNullOrWhiteSpace(ent.Comp.JobName))
            return;

        args.PushMarkup(
            $"[color={ent.Comp.Color}]" +
            $"{Loc.GetString(ent.Comp.Department)}\n" +
            $"{FormattedMessage.EscapeText(ent.Comp.FullName)}\n" +
            $"{FormattedMessage.EscapeText(ent.Comp.JobName)}\n" +
            $"№ NT-FNK-{ent.Comp.SerialNumber}" +
            "[/color]"
        );
    }
}
