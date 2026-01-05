using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.PAI;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Damage;
using Content.Shared.SS220.FieldShield;
using Content.Shared.SS220.ItemToggle;
using Content.Shared.SS220.ItemToggle;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    //[Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    //[Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    //[Dependency] private readonly EntityLookupSystem _lookup = default!;
    //[Dependency] private readonly SharedPhysicsSystem _physics = default!;
    //[Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    //[Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    //[Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeUser();

        //SS220 shield rework begin
        SubscribeLocalEvent<BlockingUserComponent, ProjectileBlockAttemptEvent>(OnBlockUserCollide);
        SubscribeLocalEvent<BlockingUserComponent, HitScanBlockAttemptEvent>(OnBlockUserHitscan);
        SubscribeLocalEvent<BlockingUserComponent, MeleeHitBlockAttemptEvent>(OnBlockUserMeleeHit);
        SubscribeLocalEvent<BlockingUserComponent, ThrowableProjectileBlockAttemptEvent>(OnBlockThrownProjectile);

        //SubscribeLocalEvent<BlockingUserComponent, BeforeThrowEvent>(OnBeforeThrow);

        //SubscribeLocalEvent<BlockingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<BlockingUserComponent, ComponentInit>(OnCompInit);
        //SS220 shield rework end

        SubscribeLocalEvent<BlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<BlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<BlockingComponent, DroppedEvent>(OnDrop);

        // SS220 equip shield on back begin
        SubscribeLocalEvent<BlockingComponent, GotEquippedEvent>(OnGotEquip);
        SubscribeLocalEvent<BlockingComponent, GotUnequippedEvent>(OnGotUnequipped);
        // SS220 equip shield on back end

        //SubscribeLocalEvent<BlockingComponent, GetItemActionsEvent>(OnGetActions);

        SubscribeLocalEvent<BlockingComponent, ComponentShutdown>(OnShutdown);

        //SubscribeLocalEvent<BlockingComponent, GetVerbsEvent<ExamineVerb>>(OnVerbExamine);
        //SubscribeLocalEvent<BlockingComponent, MapInitEvent>(OnMapInit);

        //ss220 fix drop shields start
        SubscribeLocalEvent<BlockingComponent, ThrowItemAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<BlockingComponent, ContainerGettingRemovedAttemptEvent>(OnDropAttempt);
        //ss220 fix drop shields end
    }

    //private void OnBeforeThrow(Entity<BlockingUserComponent> ent, ref BeforeThrowEvent args)
    //{
    //    if (ent.Comp.IsBlocking) { args.Cancelled=true; }
    //}

    private void OnCompInit(Entity<BlockingUserComponent> ent, ref ComponentInit args)
    {
        ChangeSeed(ent);
    }

    private void ChangeSeed(Entity<BlockingUserComponent> ent)
    {
        if (_net.IsServer)
        {
            ent.Comp.randomSeed = _random.Next(1000000);
        }
        Dirty(ent.Owner, ent.Comp);//Yes,this is probably the most obvious and dumb way to to it.
    }
    private void OnBlockUserCollide(Entity<BlockingUserComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        args.Cancelled = TryBlock(ent.Comp.BlockingItemsShields, args.Damage,ent.Comp);
    }

    private void OnBlockThrownProjectile(Entity<BlockingUserComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        args.Cancelled = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockUserHitscan(Entity<BlockingUserComponent> ent, ref HitScanBlockAttemptEvent args)
    {
        args.Cancelled = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockUserMeleeHit(Entity<BlockingUserComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        foreach (var item in ent.Comp.BlockingItemsShields)
        {
            if (!TryComp<BlockingComponent>(item, out var shield)) { return; }
            if(!TryGetNetEntity(item, out var netEnt))
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
                    args.Cancelled = true;
                    args.blocker = netEnt;
                    ChangeSeed(ent);
                    return;
                }
            }
            else
            {
                if (_random.Prob(shield.MeleeBlockProb))
                {
                    args.Cancelled = true;
                    args.blocker = netEnt;
                    ChangeSeed(ent);
                    return;
                }
            }
            ChangeSeed(ent);
            return;
        }
    }

    private bool TryBlock(List<EntityUid?> items, DamageSpecifier? damage, BlockingUserComponent comp)
    {
        foreach (var item in items)
        {
            if ((!TryComp<BlockingComponent>(item, out var shield)) || damage == null)
            {
                continue;
            }
            if (TryComp<ItemToggleBlockingDamageComponent>(item, out var toggleComp))
            {
                if(!toggleComp.IsToggled)
                {
                    continue;
                }
            }
            //if (_random.Prob(shield.RangeBlockProb))
            //{
            //    _damageable.TryChangeDamage(shield.Owner, damage);
            //    return true;
            //}
            if (comp.IsBlocking)
            {
                if (_random.Prob(shield.ActiveRangeBlockProb))
                {
                    _damageable.TryChangeDamage(shield.Owner, damage);
                    ChangeSeed((comp.Owner, comp));
                    return true;
                }
            }
            else
            {
                if (_random.Prob(shield.RangeBlockProb))
                {
                    _damageable.TryChangeDamage(shield.Owner, damage);
                    ChangeSeed((comp.Owner, comp));
                    return true;
                }
            }
        }
        ChangeSeed((comp.Owner,comp));
        return false;
    }
    //SS220 shield rework end

    //ss220 fix drop shields start
    private void OnDropAttempt(Entity<BlockingComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (IsDropBlocked(ent))
            args.Cancel();
    }

    private void OnThrowAttempt(Entity<BlockingComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (IsDropBlocked(ent))
            args.Cancelled = false;
    }

    private bool IsDropBlocked(Entity<BlockingComponent> ent)
    {
        if(!TryComp<BlockingUserComponent>(ent.Comp.User, out var userComp))
        {
            return false;
        }
        var action = userComp.BlockingToggleActionEntity;

        if (action == null || !TryComp<ActionComponent>(action.Value, out var actionComponent))
            return false;

        return _gameTiming.CurTime <= actionComponent.Cooldown?.End;
    }
    //ss220 fix drop shields end

    //ss220 raise shield activated fix start
    //ss220 raise shield activated fix end

    private void OnEquip(EntityUid uid, BlockingComponent component, GotEquippedHandEvent args)
    {
        component.User = args.User;
        Dirty(uid, component);

        //To make sure that this bodytype doesn't get set as anything but the original)
        var userComp = EnsureComp<BlockingUserComponent>(args.User);
        userComp.BlockingItemsShields.Add(uid);
        //_actionContainer.EnsureAction(args.User, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction);
        _actionsSystem.AddAction(args.User, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.User);
        Dirty(args.User, userComp);
    }

    // SS220 equip shield on back begin
    private void OnGotEquip(EntityUid uid, BlockingComponent component, GotEquippedEvent args)
    {

        if (!component.AvaliableSlots.ContainsKey(args.SlotFlags))
            return;

        component.User = args.Equipee;
        Dirty(uid, component);
        var userComp = EnsureComp<BlockingUserComponent>(args.Equipee);
        _actionsSystem.AddAction(args.Equipee, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.Equipee);
        userComp.BlockingItemsShields.Add(uid);
        Dirty(args.Equipee, userComp);
    }

    private void OnGotUnequipped(EntityUid uid, BlockingComponent component, GotUnequippedEvent args)
    {
        StopBlockingHelper(uid, component, args.Equipee);
    }
    // SS220 equip shield on back end

    private void OnUnequip(EntityUid uid, BlockingComponent component, GotUnequippedHandEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
        var userComp = EnsureComp<BlockingUserComponent>(args.User);
        //_actionsSystem.RemoveAction(userComp.BlockingToggleActionEntity);
    }

    private void OnDrop(EntityUid uid, BlockingComponent component, DroppedEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
        var userComp = EnsureComp<BlockingUserComponent>(args.User);
        //_actionsSystem.RemoveAction(userComp.BlockingToggleActionEntity);
    }

    private void OnShutdown(EntityUid uid, BlockingComponent component, ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (component.User != null)
        {
            //_actionsSystem.RemoveProvidedActions(component.User.Value, uid);
            StopBlockingHelper(uid, component, component.User.Value);
        }
    }

    /// <summary>
    /// Called where you want the user to start blocking
    /// Creates a new hard fixture to bodyblock
    /// Also makes the user static to prevent prediction issues
    /// </summary>
    /// <param name="item"> The entity with the blocking component</param>
    /// <param name="component"> The <see cref="BlockingComponent"/></param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StartBlocking(BlockingUserComponent compUser, EntityUid user)
    {
        if (compUser.IsBlocking)
            return false;

        //var xform = Transform(user);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("actively-blocking-attack");
        var msgOther = Loc.GetString("actively-blocking-others", ("blockerName", blockerName));

        //Don't allow someone to block if they're not parented to a grid
        //if (xform.GridUid != xform.ParentUid)
        //{
        //CantBlockError(user);
        //return false;
        //}

        // Don't allow someone to block if they're not holding the shield

        //Don't allow someone to block if someone else is on the same tile
        //var playerTileRef = _turf.GetTileRef(xform.Coordinates);
        //if (playerTileRef != null)
        //{
        //var intersecting = _lookup.GetLocalEntitiesIntersecting(playerTileRef.Value, 0f);
        //var mobQuery = GetEntityQuery<MobStateComponent>();
        //foreach (var uid in intersecting)
        //{
        //if (uid != user && mobQuery.HasComponent(uid))
        //{
        //TooCloseError(user);
        //return false;
        //}
        //}
        //}

        //Don't allow someone to block if they're somehow not anchored.
        //_transformSystem.AnchorEntity(user, xform);
        //if (!xform.Anchored)
        //{
        //CantBlockError(user);
        //return false;
        //}
        _actionsSystem.SetToggled(compUser.BlockingToggleActionEntity, true);
        _popupSystem.PopupPredicted(msgUser, msgOther, user, user);

        //if (TryComp<PhysicsComponent>(user, out var physicsComponent))
        //{
        //_fixtureSystem.TryCreateFixture(user,
        // component.Shape,
        //BlockingComponent.BlockFixtureID,
        //hard: true,
        //collisionLayer: (int)CollisionGroup.WallLayer,
        //body: physicsComponent);
        //}

        compUser.IsBlocking = true;
        Dirty(user, compUser);
        foreach (var shield in compUser.BlockingItemsShields)
        {
            if (shield == null) { continue; }
            ActiveBlockingEvent ev = new ActiveBlockingEvent(true);
            RaiseLocalEvent((EntityUid)shield, ev);
        }

        return true;
    }

    private void CantBlockError(EntityUid user)
    {
        var msgError = Loc.GetString("action-popup-blocking-user-cant-block");
        _popupSystem.PopupClient(msgError, user, user);
    }

    //private void TooCloseError(EntityUid user)
    //{
    //var msgError = Loc.GetString("action-popup-blocking-user-too-close");
    //_popupSystem.PopupClient(msgError, user, user);
    //}

    /// <summary>
    /// Called where you want the user to stop blocking.
    /// </summary>
    /// <param name="item"> The entity with the blocking component</param>
    /// <param name="component"> The <see cref="BlockingComponent"/></param>
    /// <param name="user"> The entity who's using the item to block</param>
    /// <returns></returns>
    public bool StopBlocking(BlockingUserComponent compUser, EntityUid user)
    {
        if (!compUser.IsBlocking)
            return false;
        //var xform = Transform(user);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("actively-blocking-stop");
        var msgOther = Loc.GetString("actively-blocking-stop-others", ("blockerName", blockerName));
        _popupSystem.PopupPredicted(msgUser, msgOther, user, user);
        _actionsSystem.SetToggled(compUser.BlockingToggleActionEntity, false);

        //If the component blocking toggle isn't null, grab the users SharedBlockingUserComponent and PhysicsComponent
        //then toggle the action to false, unanchor the user, remove the hard fixture
        //and set the users bodytype back to their original type
        //if (TryComp<BlockingUserComponent>(user, out var blockingUserComponent) && TryComp<PhysicsComponent>(user, out var physicsComponent))
        //{
        //if (xform.Anchored)
        //_transformSystem.Unanchor(user, xform);

        //_actionsSystem.SetToggled(component.BlockingToggleActionEntity, false);
        //_fixtureSystem.DestroyFixture(user, BlockingComponent.BlockFixtureID, body: physicsComponent);
        //_physics.SetBodyType(user, blockingUserComponent.OriginalBodyType, body: physicsComponent);
        //_popupSystem.PopupPredicted(msgUser, msgOther, user, user);
        //}
        compUser.IsBlocking = false;
        Dirty(user, compUser);
        foreach (var shield in compUser.BlockingItemsShields)
        {
            if (shield == null) { continue; }
            ActiveBlockingEvent ev = new ActiveBlockingEvent(false);
            RaiseLocalEvent((EntityUid)shield, ev);
        }

        return true;
    }

    /// <summary>
    /// Called where you want someone to stop blocking and to remove the <see cref="BlockingUserComponent"/> from them
    /// Won't remove the <see cref="BlockingUserComponent"/> if they're holding another blocking item
    /// </summary>
    /// <param name="uid"> The item the component is attached to</param>
    /// <param name="component"> The <see cref="BlockingComponent"/> </param>
    /// <param name="user"> The person holding the blocking item </param>
    private void StopBlockingHelper(EntityUid uid, BlockingComponent component, EntityUid user)
    {
        var userQuery = GetEntityQuery<BlockingUserComponent>();
        if(!userQuery.TryGetComponent(user, out var component1)) { return; }
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
            if (HasComp<BlockingComponent>(shield) && userQuery.TryGetComponent(user, out var blockingUserComponent))
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
                RemComp<BlockingUserComponent>(user);
            }
        }
    }
}

[Serializable, NetSerializable]
public sealed class ActiveBlockingEvent(bool active) : EntityEventArgs
{
    public bool Active = active;
}
