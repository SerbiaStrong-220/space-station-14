// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.VendingMachines;

public abstract partial class SharedVendingMachineSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void ReturnInitialize()
    {
        SubscribeLocalEvent<VendingMachineComponent, AfterInteractUsingEvent>(HandleAfterInteractUsing);
    }

    private void HandleAfterInteractUsing(Entity<VendingMachineComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled || !args.CanReach)
            return;

        var (uid, component) = entity;

        if (!HasComp<HandsComponent>(args.User))
            return;

        if (!IsAuthorized(uid, args.User, component))
            return;

        if (!TryInsertItem(entity, args.User, args.Used))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Used)} into {ToPrettyString(uid)}");
        Dirty(entity);
        args.Handled = true;
    }

    private bool TryInsertItem(Entity<VendingMachineComponent> vend, EntityUid userUid, EntityUid entityUid)
    {
        if (!_containerSystem.TryGetContainer(vend.Owner, VendingMachineComponent.ContainerId, out var vendContainer))
            return false;

        if (!TryGetItemCode(entityUid, out var itemId))
            return false;

        if (!ItemIsInWhitelist(entityUid, itemId, vend.Comp))
            return false;

        if (!_handsSystem.IsHolding(userUid, entityUid, out var hand) || !_handsSystem.CanDropHeld(userUid, hand))
            return false;

        if (vend.Comp.Ejecting || vend.Comp.Broken || !_receiver.IsPowered(vend.Owner))
            return false;

        if (!_handsSystem.TryDropIntoContainer(userUid, entityUid, vendContainer))
            return false;

        if (vend.Comp.Inventory.ContainsKey(itemId)
            && vend.Comp.Inventory.TryGetValue(itemId, out var entry))
        {
            entry.Amount++;
            entry.EntityUids.Add(GetNetEntity(entityUid));
            return true;
        }

        vend.Comp.Inventory.Add(itemId,
            new VendingMachineInventoryEntry(InventoryType.Regular, itemId, 1)
            {
                EntityUids = new() { GetNetEntity(entityUid) },
            }
        );

        return true;
    }

    private bool ItemIsInWhitelist(EntityUid item, string itemCode, VendingMachineComponent vendComponent)
    {
        if (vendComponent.Inventory.ContainsKey(itemCode))
            return true;

        return vendComponent.Whitelist != null && _entityWhitelist.IsWhitelistPass(vendComponent.Whitelist, item);
    }

    private bool TryGetItemCode(EntityUid entityUid, out string code)
    {
        var metadata = IoCManager.Resolve<IEntityManager>().GetComponentOrNull<MetaDataComponent>(entityUid);
        code = metadata?.EntityPrototype?.ID ?? "";
        return !string.IsNullOrEmpty(code);
    }

}
