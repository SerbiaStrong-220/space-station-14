using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared.StationRecords;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.SS220.SecHudRecords;

public abstract class SharedSecHudRecordsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    private const string EyesSlot = "eyes";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<StatusIconComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        if (!_inv.TryGetSlotEntity(args.User, EyesSlot, out var glasses) ||
            !TryComp<SecHudRecordsComponent>(glasses.Value, out var secHudRecords))
            return;

        if (!_idCard.TryFindIdCard(args.Target, out var idCard))
            return;

        if (!TryComp<StationRecordKeyStorageComponent>(idCard, out var storage))
            return;

        var key = storage.Key;
        if (key == null)
            return;

        var netTarget = GetNetEntity(args.Target);
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                VerbAct(netTarget, actor.PlayerSession, key.Value);
            },
            Text = Loc.GetString("sec-hud-records-change-status"),
            Category = VerbCategory.Examine,
            Icon = secHudRecords.VerbSprite,
        };

        args.Verbs.Add(verb);
    }

    protected abstract void VerbAct(NetEntity target, ICommonSession session, StationRecordKey key);
}
