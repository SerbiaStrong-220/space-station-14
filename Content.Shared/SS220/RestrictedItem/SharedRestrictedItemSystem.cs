// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.RestrictedItem;

public abstract class SharedRestrictedItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RestrictedItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<RestrictedItemComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<RestrictedItemComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<DropAllRestrictedEvent>(OnDropAll);
    }

    private void OnPickupAttempt(Entity<RestrictedItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!ItemCheck(args.User, ent))
            args.Cancel();
    }

    private void OnPullAttempt(Entity<RestrictedItemComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (ent.Comp.CanBePulled)
            return;

        if (!ItemCheck(args.Puller, ent))
            args.Cancel();
    }

    private void OnEquipAttempt(Entity<RestrictedItemComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!ItemCheck(args.EquipTarget, ent))
            args.Cancel();
    }

    protected bool ItemCheck(EntityUid user, Entity<RestrictedItemComponent> item)
    {
        if (_whitelistSystem.IsWhitelistFail(item.Comp.Whitelist, user))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString(item.Comp.LocalizedPopup), item);

            if (!item.Comp.DamageOnFail.Empty)
                _damageable.TryChangeDamage(user, item.Comp.DamageOnFail, true);

            _audio.PlayPredicted(item.Comp.SoundOnFail, item, user);

            return false;
        }

        return true;
    }

    private void OnDropAll(ref DropAllRestrictedEvent ev)
    {
        var removedItems = RemoveItems(ev.Target);
        ev.DroppedItems.UnionWith(removedItems);
    }

    private HashSet<EntityUid> RemoveItems(EntityUid target)
    {
        HashSet<EntityUid> removedItems = [];
        if (!_inventory.TryGetSlots(target, out _))
            return removedItems;

        // trying to unequip all item's with component
        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<RestrictedItemComponent>(item, out var restrictedComp)) //ToDo_SS220 make check for a whitelist
                continue;

            _transform.DropNextTo(item, target);
            removedItems.Add(item);
        }

        return removedItems;
    }
}

/// <summary>
///     Raised when we need to remove all Restricted objects
/// </summary>
[ByRefEvent, Serializable]
public sealed class DropAllRestrictedEvent(EntityUid target, HashSet<EntityUid>? droppedItems = null) : EntityEventArgs
{
    public readonly EntityUid Target = target;

    public HashSet<EntityUid> DroppedItems = droppedItems ?? new();
}
