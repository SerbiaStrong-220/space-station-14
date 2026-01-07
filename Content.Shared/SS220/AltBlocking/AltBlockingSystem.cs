using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.SS220.ItemToggle;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class AltBlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeUser();

        SubscribeLocalEvent<AltBlockingUserComponent, ProjectileBlockAttemptEvent>(OnBlockUserCollide);
        SubscribeLocalEvent<AltBlockingUserComponent, HitscanBlockAttemptEvent>(OnBlockUserHitscan);
        SubscribeLocalEvent<AltBlockingUserComponent, MeleeHitBlockAttemptEvent>(OnBlockUserMeleeHit);
        SubscribeLocalEvent<AltBlockingUserComponent, ThrowableProjectileBlockAttemptEvent>(OnBlockThrownProjectile);

        SubscribeLocalEvent<AltBlockingUserComponent, ComponentInit>(OnCompInit);

        SubscribeLocalEvent<AltBlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<AltBlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<AltBlockingComponent, DroppedEvent>(OnDrop);

        SubscribeLocalEvent<AltBlockingComponent, GotEquippedEvent>(OnGotEquip);
        SubscribeLocalEvent<AltBlockingComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<AltBlockingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<AltBlockingComponent, ThrowItemAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<AltBlockingComponent, ContainerGettingRemovedAttemptEvent>(OnDropAttempt);
    }
    private void OnCompInit(Entity<AltBlockingUserComponent> ent, ref ComponentInit args)
    {
        ChangeSeed(ent);
    }

    private void ChangeSeed(Entity<AltBlockingUserComponent> ent)
    {
        if (_net.IsServer)
        {
            ent.Comp.randomSeed = _random.Next(1000000);
        }
        Dirty(ent.Owner, ent.Comp);//Yes,this is probably the most obvious and dumb way to to it.
    }
    private void OnBlockUserCollide(Entity<AltBlockingUserComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockThrownProjectile(Entity<AltBlockingUserComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        args.Cancelled = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockUserHitscan(Entity<AltBlockingUserComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockUserMeleeHit(Entity<AltBlockingUserComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        foreach (var item in ent.Comp.BlockingItemsShields)
        {
            if (!TryComp<AltBlockingComponent>(item, out var shield)) { return; }
            if (!TryGetNetEntity(item, out var netEnt))
            {
                return;
            }
            if (TryComp<ItemToggleBlockingDamageComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                {
                    continue;
                }
            }
            _random.SetSeed(ent.Comp.randomSeed);
            if (ent.Comp.IsBlocking)
            {
                if (_random.Prob(shield.ActiveMeleeBlockProb))
                {
                    args.CancelledHit = true;
                    args.blocker = netEnt;
                    ChangeSeed(ent);
                    return;
                }
            }
            else
            {
                if (_random.Prob(shield.MeleeBlockProb))
                {
                    args.CancelledHit = true;
                    args.blocker = netEnt;
                    ChangeSeed(ent);
                    return;
                }
            }
            ChangeSeed(ent);
        }
        return;
    }

    private bool TryBlock(List<EntityUid?> items, DamageSpecifier? damage, AltBlockingUserComponent comp)
    {
        foreach (var item in items)
        {
            if ((!TryComp<AltBlockingComponent>(item, out var shield)) || damage == null)
            {
                continue;
            }
            if (TryComp<ItemToggleBlockingDamageComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                {
                    continue;
                }
            }
            _random.SetSeed(comp.randomSeed);
            if (comp.IsBlocking)
            {
                if (_random.Prob(shield.ActiveRangeBlockProb))
                {
                    _damageable.TryChangeDamage(shield.Owner, damage);
                    ChangeSeed((comp.Owner, comp));
                    _audio.PlayPvs(shield.BlockSound, (EntityUid)item);
                    return true;
                }
            }
            else
            {
                if (_random.Prob(shield.RangeBlockProb))
                {
                    _damageable.TryChangeDamage(shield.Owner, damage);
                    ChangeSeed((comp.Owner, comp));
                    _audio.PlayPvs(shield.BlockSound, (EntityUid)item);
                    return true;
                }
            }
            ChangeSeed((comp.Owner, comp));
        }
        return false;
    }
    private void OnDropAttempt(Entity<AltBlockingComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (IsDropBlocked(ent))
            args.Cancel();
    }

    private void OnThrowAttempt(Entity<AltBlockingComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (IsDropBlocked(ent))
            args.Cancelled = false;
    }

    private bool IsDropBlocked(Entity<AltBlockingComponent> ent)
    {
        if (!TryComp<AltBlockingUserComponent>(ent.Comp.User, out var userComp))
        {
            return false;
        }
        var action = userComp.BlockingToggleActionEntity;

        if (action == null || !TryComp<ActionComponent>(action.Value, out var actionComponent))
            return false;

        return _gameTiming.CurTime <= actionComponent.Cooldown?.End;
    }

    private void OnEquip(EntityUid uid, AltBlockingComponent component, GotEquippedHandEvent args)
    {
        component.User = args.User;
        Dirty(uid, component);
        var userComp = EnsureComp<AltBlockingUserComponent>(args.User);
        userComp.BlockingItemsShields.Add(uid);
        _actionsSystem.AddAction(args.User, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.User);
        Dirty(args.User, userComp);
    }
    private void OnGotEquip(EntityUid uid, AltBlockingComponent component, GotEquippedEvent args)
    {

        if (!component.AvaliableSlots.ContainsKey(args.SlotFlags))
            return;

        component.User = args.Equipee;
        Dirty(uid, component);
        var userComp = EnsureComp<AltBlockingUserComponent>(args.Equipee);
        _actionsSystem.AddAction(args.Equipee, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.Equipee);
        userComp.BlockingItemsShields.Add(uid);
        Dirty(args.Equipee, userComp);
    }

    private void OnGotUnequipped(EntityUid uid, AltBlockingComponent component, GotUnequippedEvent args)
    {
        StopBlockingHelper(uid, component, args.Equipee);
    }

    private void OnUnequip(EntityUid uid, AltBlockingComponent component, GotUnequippedHandEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    private void OnDrop(EntityUid uid, AltBlockingComponent component, DroppedEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    private void OnShutdown(EntityUid uid, AltBlockingComponent component, ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (component.User != null)
        {
            StopBlockingHelper(uid, component, component.User.Value);
        }
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
            if (shield == null) { continue; }
            if (TryComp<ItemToggleBlockingDamageComponent>(shield, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                {
                    continue;
                }
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
            if (shield == null) { continue; }
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
        if (!userQuery.TryGetComponent(user, out var component1)) { return; }
        if (component1.IsBlocking)
            StopBlocking(component1, user);
        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(user, out var hands))
            return;

        var shields = _handsSystem.EnumerateHeld((user, hands)).ToArray();

        if (component1 != null && component1.BlockingItemsShields.Contains(uid))
        {
            component1.BlockingItemsShields.Remove(uid);
        }

        foreach (var shield in shields)
        {
            if (HasComp<AltBlockingComponent>(shield) && userQuery.TryGetComponent(user, out var AltBlockingUserComponent))
            {
                return;
            }
        }
        component.User = null;
        if (component1 != null)
        {
            component1.BlockingItemsShields.Clear();
            if (_net.IsServer)
            {
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
