// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Managers;
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
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RestrictedItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<RestrictedItemComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<RestrictedItemComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(Entity<RestrictedItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!CanInteract(args.User, ent))
            args.Cancel();
    }

    private void OnPullAttempt(Entity<RestrictedItemComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (ent.Comp.CanBePulled)
            return;

        if (!CanInteract(args.Puller, ent))
            args.Cancel();
    }

    private void OnEquipAttempt(Entity<RestrictedItemComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!CanInteract(args.EquipTarget, ent))
            args.Cancel();
    }

    protected bool CanInteract(EntityUid user, Entity<RestrictedItemComponent> item)
    {
        if (_adminManager.IsAdmin(user))
            return true;

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
    public void DropAllRestrictedItems(EntityUid ent)
    {
        if (!_inventory.TryGetSlots(ent, out _))
            return;

        // trying to unequip all item's with component
        foreach (var item in _inventory.GetHandOrInventoryEntities(ent))
        {
            if (!TryComp<RestrictedItemComponent>(item, out var restrictedComp)) //ToDo_SS220 make check for a whitelist
                continue;

            _transform.DropNextTo(item, ent);
        }
    }
}
