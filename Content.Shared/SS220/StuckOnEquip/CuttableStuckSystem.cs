// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.SS220.StuckOnEquip;

public sealed partial class CuttableStuckSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedStuckOnEquipSystem _stuck = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CuttableStuckComponent, GetVerbsEvent<EquipmentVerb>>(OnItemVerbs);
        SubscribeLocalEvent<CuttableStuckComponent, CutStuckDoAfterEvent>(OnCutFinished);
        SubscribeLocalEvent<CuttableStuckComponent, DoAfterAttemptEvent<CutStuckDoAfterEvent>>(OnCutAttempt);
    }

    private void OnItemVerbs(Entity<CuttableStuckComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        // Generic equipment access excludes another person's hand slots; CanCut checks the wearer and item together.
        if (!args.CanInteract)
            return;

        if (args.Using == null)
            return;

        var tool = args.Using.Value;
        if (!CanCut(args.User, ent, tool, out _, out _))
            return;

        var user = args.User;
        args.Verbs.Add(new EquipmentVerb
        {
            Text = Loc.GetString("cuttable-stuck-verb"),
            Act = () => TryStartCut(user, (ent.Owner, ent.Comp), tool),
        });
    }

    /// <summary>
    /// Starts cutting from the attachment's own verb menu, rechecking access and the held sharp tool.
    /// </summary>
    public bool TryStartCut(EntityUid user, Entity<CuttableStuckComponent?> item, EntityUid tool)
    {
        if (!Resolve(item.Owner, ref item.Comp, false))
            return false;

        if (!CanCut(user, item, tool, out var wearer, out var containerId))
            return false;

        var args = new DoAfterArgs(
            EntityManager,
            user,
            item.Comp.Delay,
            new CutStuckDoAfterEvent(containerId),
            item,
            target: wearer,
            used: tool)
        {
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        var message = Loc.GetString("cuttable-stuck-start", ("user", user), ("item", item.Owner), ("wearer", wearer));
        _popup.PopupPredicted(message, wearer, user);
        return true;
    }

    private bool CanCut(EntityUid user, EntityUid item, EntityUid tool, out EntityUid wearer, out string containerId)
    {
        wearer = default;
        containerId = string.Empty;

        if (_hands.GetActiveItem(user) != tool)
            return false;

        if (!HasComp<SharpComponent>(tool))
            return false;

        if (!_blocker.CanUseHeldEntity(user, tool))
            return false;

        if (!TryComp<StuckOnEquipComponent>(item, out var stuck))
            return false;

        if (!stuck.IsStuck)
            return false;

        if (!_containers.TryGetContainingContainer((item, null, null), out var container))
            return false;

        wearer = container.Owner;
        containerId = container.ID;
        if (!HasComp<DamageableComponent>(wearer))
            return false;

        if (!_blocker.CanInteract(user, wearer))
            return false;

        return _inventory.CanAccess(user, wearer, item);
    }

    private bool CanContinue(EntityUid item, DoAfterArgs args, CutStuckDoAfterEvent ev)
    {
        if (args.Used == null)
            return false;

        if (!CanCut(args.User, item, args.Used.Value, out var wearer, out var containerId))
            return false;

        if (wearer != args.Target)
            return false;

        return containerId == ev.ContainerId;
    }

    private void OnCutAttempt(Entity<CuttableStuckComponent> ent, ref DoAfterAttemptEvent<CutStuckDoAfterEvent> args)
    {
        if (!CanContinue(ent, args.DoAfter.Args, args.Event))
            args.Cancel();
    }

    private void OnCutFinished(Entity<CuttableStuckComponent> ent, ref CutStuckDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (_net.IsClient)
            return;

        if (!CanContinue(ent, args.Args, args))
            return;

        if (args.Target == null)
            return;

        var wearer = args.Target.Value;
        if (!TryComp<StuckOnEquipComponent>(ent, out var stuck))
            return;

        args.Handled = true;
        if (!_stuck.TryRemoveItem((ent.Owner, stuck), args.User))
            return;

        // The blade cuts the attachment at the body, underneath any worn protection.
        var damage = _damage.ChangeDamage(wearer, ent.Comp.Damage, ignoreResistances: true, origin: args.User);
        var message = Loc.GetString(
            "cuttable-stuck-finish",
            ("user", args.User),
            ("item", ent.Owner),
            ("wearer", wearer));
        _popup.PopupEntity(message, wearer);
        _adminLogger.Add(LogType.Stripping, LogImpact.High,
            $"{ToPrettyString(args.User):actor} cut {ToPrettyString(ent):item} off {ToPrettyString(wearer):target} " +
            $"with {ToPrettyString(args.Used):tool}, dealing {damage.GetTotal()} damage");
    }
}
