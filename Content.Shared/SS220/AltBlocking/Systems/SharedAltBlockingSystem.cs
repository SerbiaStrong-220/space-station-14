// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.ToggleBlocking;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Alert;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

        //SubscribeLocalEvent<AltBlockingComponent, ThrowItemAttemptEvent>(OnThrowAttempt); // didn't remove these just in case i want to get them back
        //SubscribeLocalEvent<AltBlockingComponent, ContainerGettingRemovedAttemptEvent>(OnDropAttempt);
    }

    /// <summary>
    /// Called where you want the user to start blocking
    /// </summary>
    /// <param name="compUsert"> The <see cref="AltBlockingUserComponent"/></param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StartBlocking(Entity<AltBlockingUserComponent> ent)
    {
        if (ent.Comp.IsBlocking)
            return false;

        var blockerName = Identity.Entity(ent.Owner, EntityManager);
        var msgUser = Loc.GetString("actively-blocking-attack");
        var msgOther = Loc.GetString("actively-blocking-others", ("blockerName", blockerName));

        _popupSystem.PopupPredicted(msgUser, msgOther, ent.Owner, ent.Owner);

        ent.Comp.IsBlocking = true;
        Dirty(ent);

        _alerts.ShowAlert(ent.Owner, ent.Comp.BlockingAlertProtoId, 0);

        foreach (var shield in ent.Comp.BlockingItemsShields)
        {
            if (shield == null)
                continue;

            if (TryComp<AltBlockingComponent>(shield, out var blockComp))
                blockComp.IsBlocking = true;

            if (TryComp<ToggleBlockingChanceComponent>(shield, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                    continue;
            }

            ActiveBlockingEvent ev = new ActiveBlockingEvent(true);
            RaiseLocalEvent((EntityUid)shield, ref ev);
        }
        return true;
    }

    /// <summary>
    /// Called where you want the user to stop blocking.
    /// </summary>
    /// <param name="compUsert"> The <see cref="AltBlockingUserComponent"/></param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StopBlocking(Entity<AltBlockingUserComponent> ent)
    {
        if (!ent.Comp.IsBlocking)
            return false;

        var blockerName = Identity.Entity(ent.Owner, EntityManager);

        var msgUser = Loc.GetString("actively-blocking-stop");
        var msgOther = Loc.GetString("actively-blocking-stop-others", ("blockerName", blockerName));

        _popupSystem.PopupPredicted(msgUser, msgOther, ent.Owner, ent.Owner);

        ent.Comp.IsBlocking = false;
        Dirty(ent);

        _alerts.ClearAlert(ent.Owner, ent.Comp.BlockingAlertProtoId);

        foreach (var shield in ent.Comp.BlockingItemsShields)
        {
            if (shield == null)
                continue;

            if (TryComp<AltBlockingComponent>(shield, out var blockComp))
                blockComp.IsBlocking = false;

            ActiveBlockingEvent ev = new ActiveBlockingEvent(false);
            RaiseLocalEvent((EntityUid)shield, ref ev);
        }

        return true;
    }

    /// <summary>
    /// Called where you want someone to stop blocking and to remove the <see cref="AltBlockingUserComponent"/> from them
    /// Won't remove the <see cref="AltBlockingUserComponent"/> if they're holding another blocking item
    /// </summary>
    /// <param name="uid"> The item the component is attached to</param>
    /// <param name="component"> The <see cref="AltBlockingComponent"/> </param>
    /// <param name="user"> The person holding the blocking item </param>
    private void StopBlockingHelper(Entity<AltBlockingComponent> ent, EntityUid user)
    {
        var userQuery = GetEntityQuery<AltBlockingUserComponent>();

        if (!userQuery.TryGetComponent(user, out var componentUser))
            return;

        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(user, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld((user, hands)).ToArray();

        if (componentUser != null && componentUser.BlockingItemsShields.Contains(ent.Owner))
            componentUser.BlockingItemsShields.Remove(ent.Owner);

        if (TryComp<ArmorBlockComponent>(ent.Owner, out var armorComp))
            armorComp.User = null;

        ent.Comp.User = null;

        foreach (var shield in shields)
        {
            if (HasComp<AltBlockingComponent>(shield) && userQuery.TryGetComponent(user, out var _))
                return;
        }

        if (componentUser != null)
        {
            componentUser.BlockingItemsShields.Clear();
            if (_net.IsServer)
            {
                if (componentUser.IsBlocking)
                    StopBlocking((user,componentUser));

                RemComp<AltBlockingUserComponent>(user);
            }
        }
    }
}

[ByRefEvent]
public record struct ActiveBlockingEvent(bool active)
{
    public bool Active = active;
}
