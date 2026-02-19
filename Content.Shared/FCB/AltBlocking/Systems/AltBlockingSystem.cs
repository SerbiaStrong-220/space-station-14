// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FCB.ToggleBlocking;
using Content.Shared.FCB.Weapons.Melee.Events;
using Content.Shared.FCB.Weapons.Ranged;
using Content.Shared.FCB.Weapons.Ranged.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
//using Content.Shared.Weapons.Hitscan.Components; It is made for the upstream,just decomment it then
//using Content.Shared.Weapons.Hitscan.Events; 
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.FCB.AltBlocking;

public sealed partial class AltBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeUser();

        SubscribeLocalEvent<AltBlockingUserComponent, ProjectileBlockAttemptEvent>(OnBlockUserCollide);
        //SubscribeLocalEvent<HitscanBasicDamageComponent, AttemptHitscanRaycastFiredEvent>(OnBlockUserHitscan);
        SubscribeLocalEvent<AltBlockingUserComponent, HitscanBlockAttemptEvent>(OnBlockUserHitscan);
        SubscribeLocalEvent<AltBlockingUserComponent, MeleeHitBlockAttemptEvent>(OnBlockUserMeleeHit);
        SubscribeLocalEvent<AltBlockingUserComponent, ThrowableProjectileBlockAttemptEvent>(OnBlockThrownProjectile);

        SubscribeLocalEvent<AltBlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<AltBlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<AltBlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<AltBlockingComponent, GotEquippedEvent>(OnGotEquip);
        SubscribeLocalEvent<AltBlockingComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<AltBlockingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<AltBlockingComponent, ThrowItemAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<AltBlockingComponent, ContainerGettingRemovedAttemptEvent>(OnDropAttempt);
    }

    private bool IsDropBlocked(Entity<AltBlockingComponent> ent)
    {
        if (!TryComp<AltBlockingUserComponent>(ent.Comp.User, out var userComp))
            return false;

        var action = userComp.BlockingToggleActionEntity;

        if (action == null || !TryComp<ActionComponent>(action.Value, out var actionComponent))
            return false;

        return _gameTiming.CurTime <= actionComponent.Cooldown?.End;
    }

    /// <summary>
    /// Called where you want the user to start blocking
    /// </summary>
    /// <param name="compUsert"> The <see cref="AltBlockingUserComponent"/></param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StartBlocking(AltBlockingUserComponent compUser, EntityUid user)//SS220 shield rework
    {
        if (compUser.IsBlocking)
            return false;

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("actively-blocking-attack");
        var msgOther = Loc.GetString("actively-blocking-others", ("blockerName", blockerName));

        _actionsSystem.SetToggled(compUser.BlockingToggleActionEntity, true);
        _popupSystem.PopupPredicted(msgUser, msgOther, user, user);

        compUser.IsBlocking = true;
        Dirty(user, compUser);

        foreach (var shield in compUser.BlockingItemsShields)
        {
            if (shield == null)
                continue;

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
    public bool StopBlocking(AltBlockingUserComponent compUser, EntityUid user)
    {
        if (!compUser.IsBlocking)
            return false;

        var blockerName = Identity.Entity(user, EntityManager);

        var msgUser = Loc.GetString("actively-blocking-stop");
        var msgOther = Loc.GetString("actively-blocking-stop-others", ("blockerName", blockerName));

        _popupSystem.PopupPredicted(msgUser, msgOther, user, user);
        _actionsSystem.SetToggled(compUser.BlockingToggleActionEntity, false);

        compUser.IsBlocking = false;
        Dirty(user, compUser);

        foreach (var shield in compUser.BlockingItemsShields)
        {
            if (shield == null)
                continue;

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
    private void StopBlockingHelper(EntityUid uid, AltBlockingComponent component, EntityUid user)
    {
        var userQuery = GetEntityQuery<AltBlockingUserComponent>();

        if (!userQuery.TryGetComponent(user, out var component1))
            return;

        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(user, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld((user, hands)).ToArray();

        if (component1 != null && component1.BlockingItemsShields.Contains(uid))
            component1.BlockingItemsShields.Remove(uid);

        foreach (var shield in shields)
        {
            if (HasComp<AltBlockingComponent>(shield) && userQuery.TryGetComponent(user, out var AltBlockingUserComponent))
                return;
        }

        component.User = null;
        if (component1 != null)
        {
            component1.BlockingItemsShields.Clear();
            if (_net.IsServer)
            {
                if (component1.IsBlocking)
                    StopBlocking(component1, user);

                _actionsSystem.RemoveAction(component1.BlockingToggleActionEntity);
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
