// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.SS220.Ghost;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CriminalRecords;

public abstract class SharedCriminalRecordSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, ExaminedEvent>(OnStatusExamine);
    }

    // TheArturZh 25.09.2023 22:15
    // TODO: bad code. make it use InventoryRelayedEvent. Create separate components for examining and for examined subscription.
    // no pohuy prosto zaebalsya(
    private void OnStatusExamine(Entity<StatusIconComponent> ent, ref ExaminedEvent args)
    {
        var scannerOn = false;

        // SS220 ADD GHOST HUD'S START
        if (HasComp<GhostComponent>(args.Examiner) && HasComp<GhostHudOnOtherComponent>(args.Examiner))
        {
            if (HasComp<ShowCriminalRecordIconsComponent>(args.Examiner))
            {
                scannerOn = true;
            }
        }
        // SS220 ADD GHOST HUD'S END

        if (_inventory.TryGetSlotEntity(args.Examiner, "eyes", out var eyesSlotEntity))
        {
            if (HasComp<ShowCriminalRecordIconsComponent>(eyesSlotEntity))
                scannerOn = true;
        }

        if (!scannerOn)
            return;

        CriminalRecord? record = null;

        if (_accessReader.FindAccessItemsInventory(ent.Owner, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp<IdCardComponent>(item, out var id))
                {
                    if (id.CurrentSecurityRecord != null)
                    {
                        record = id.CurrentSecurityRecord;
                        break;
                    }
                }

                // PDA
                if (TryComp<PdaComponent>(item, out var pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    if (id.CurrentSecurityRecord != null)
                    {
                        record = id.CurrentSecurityRecord;
                        break;
                    }
                }
            }
        }

        //SS220 Criminal-Records begin
        if (record != null)
        {
            var msg = new FormattedMessage();

            if (record.RecordType == null)
            {
                msg.AddMarkupOrThrow(Loc.GetString("criminal-show-record-type-null"));
            }
            else
            {
                if (_prototype.TryIndex(record.RecordType, out var statusType))
                {
                    msg.AddMarkupOrThrow(Loc.GetString("criminal-show-record-type-found", ("color", statusType.Color.ToHex()), ("name", statusType.Name)));
                }
            }

            msg.AddText(record.Message);
            args.PushMessage(msg);
        }
    }
}
