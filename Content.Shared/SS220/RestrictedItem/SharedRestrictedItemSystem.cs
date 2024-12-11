// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.RestrictedItem;

public abstract class SharedRestrictedItemSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RestrictedItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<RestrictedItemComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<RestrictedItemComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(Entity<RestrictedItemComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!ItemCheck(args.User, ent))
            args.Cancel();
    }

    private void OnPullAttempt(Entity<RestrictedItemComponent> ent, ref BeingPulledAttemptEvent args)
    {
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
}
