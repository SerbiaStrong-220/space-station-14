// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.ToggleBlocking;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Throwing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;


namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem : EntitySystem
{
    private static readonly string ActiveBlockingOwnerLocale = "actively-blocking-attack";
    private static readonly string ActiveBlockingOthersLocale = "actively-blocking-others";
    private static readonly string StopActiveBlockingOwnerLocale = "actively-blocking-stop";
    private static readonly string StopActiveBlockingOthersLocale = "actively-blocking-stop-others";
    private static readonly string BlockShotLocale = "block-shot";
    private static readonly string BlockThrowingLocale = "throwing-block";

    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeUser();

        SubscribeLocalEvent<AltBlockingUserComponent, ProjectileBlockAttemptEvent>(OnBlockUserCollide);
        SubscribeLocalEvent<AltBlockingUserComponent, HitscanBlockAttemptEvent>(OnBlockUserHitscan);
        SubscribeLocalEvent<AltBlockingUserComponent, MeleeHitBlockAttemptEvent>(OnBlockUserMeleeHit);
        SubscribeLocalEvent<AltBlockingUserComponent, ThrowableProjectileBlockAttemptEvent>(OnBlockThrownProjectile);

        SubscribeLocalEvent<AltBlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<AltBlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<AltBlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<AltBlockingComponent, ComponentShutdown>(OnShutdown);

        //SubscribeLocalEvent<AltBlockingComponent, ContainerGettingRemovedAttemptEvent>(OnDropAttempt);
    }

    public bool TryStartBlocking(Entity<AltBlockingUserComponent> ent)
    {
        if (ent.Comp.Blocking)
            return true;

        var blockerName = Identity.Entity(ent.Owner, EntityManager);
        var msgUser = Loc.GetString(ActiveBlockingOwnerLocale);
        var msgOther = Loc.GetString(ActiveBlockingOthersLocale, ("blockerName", blockerName));

        _popupSystem.PopupPredicted(msgUser, msgOther, ent.Owner, ent.Owner);

        ent.Comp.Blocking = true;
        Dirty(ent);

        _alerts.ShowAlert(ent.Owner, ent.Comp.BlockingAlertProtoId, 0);

        foreach (var shield in ent.Comp.BlockingItemsShields)
        {
            if (TryComp<ToggleBlockingChanceComponent>(shield, out var toggleComp)
                && !toggleComp.Toggled)
                continue;

            ActiveBlockingStateChanged ev = new ActiveBlockingStateChanged(true);
            RaiseLocalEvent(shield, ref ev);
        }
        return true;
    }

    public bool StopBlocking(Entity<AltBlockingUserComponent> ent)
    {
        if (!ent.Comp.Blocking)
            return false;

        var blockerName = Identity.Entity(ent.Owner, EntityManager);

        var msgUser = Loc.GetString(StopActiveBlockingOwnerLocale);
        var msgOther = Loc.GetString(StopActiveBlockingOthersLocale, ("blockerName", blockerName));

        _popupSystem.PopupPredicted(msgUser, msgOther, ent.Owner, ent.Owner);

        ent.Comp.Blocking = false;
        Dirty(ent);

        _alerts.ClearAlert(ent.Owner, ent.Comp.BlockingAlertProtoId);

        foreach (var shield in ent.Comp.BlockingItemsShields)
        {
            ActiveBlockingStateChanged ev = new ActiveBlockingStateChanged(false);
            RaiseLocalEvent(shield, ref ev);
        }

        return true;
    }

    private void StopBlockingHelper(Entity<AltBlockingComponent> ent, EntityUid user)
    {
        if (!TryComp<AltBlockingUserComponent>(user, out var userComp))
            return;

        if (!TryComp<HandsComponent>(user, out var handsComp))
            return;

        var heldItems = _handsSystem.EnumerateHeld((user, handsComp)).ToArray();

        if (userComp != null && userComp.BlockingItemsShields.Contains(ent.Owner))
            userComp.BlockingItemsShields.Remove(ent.Owner);

        if (TryComp<ArmorBlockComponent>(ent.Owner, out var armorComp))
            armorComp.User = null;

        ent.Comp.User = null;

        foreach (var item in heldItems)
        {
            if (HasComp<AltBlockingComponent>(item))
                return;
        }

        if (userComp != null
            && _net.IsServer)
        {
            if (userComp.Blocking)
                StopBlocking((user, userComp));

            RemComp<AltBlockingUserComponent>(user);
        }
    }
}

[ByRefEvent]
public record struct ActiveBlockingStateChanged(bool State) { }
