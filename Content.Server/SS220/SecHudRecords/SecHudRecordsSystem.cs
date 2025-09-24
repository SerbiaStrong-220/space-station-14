using Content.Server.Access.Systems;
using Content.Server.EUI;
using Content.Server.SS220.CriminalRecords;
using Content.Server.SS220.SecHudRecords.EUI;
using Content.Shared.Inventory;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.SS220.SecHudRecords;
using Content.Shared.StationRecords;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.SecHudRecords;

public sealed class SecHudRecordsSystem : SharedSecHudRecordsSystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly CriminalRecordSystem _record = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly InventorySystem _inv = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<StatusIconComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!_inv.TryGetSlotEntity(args.User, "eyes", out var glasses) ||
            !HasComp<SecHudRecordsComponent>(glasses.Value))
            return;

        if (!_idCard.TryFindIdCard(args.Target, out var idCard))
            return;

        if (!TryComp<StationRecordKeyStorageComponent>(idCard, out var storage))
            return;

        var key = storage.Key;
        if (key == null)
            return;

        List<(ProtoId<CriminalStatusPrototype>?, string)> fullCatalog = new();

        if (_record.GetRecordCatalog(key.Value, out var catalog))
        {
            foreach (var record in catalog.Records)
            {
                fullCatalog.Add((record.Value.RecordType, record.Value.Message));
            }
        }

        var netTarget = GetNetEntity(args.Target);
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                _eui.OpenEui(new SecHudRecordsEui(netTarget, fullCatalog), actor.PlayerSession);
            },
            Text = Loc.GetString("sec-hud-records-change-status"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/protection_glasses.png"))
        };

        args.Verbs.Add(verb);
    }
}
