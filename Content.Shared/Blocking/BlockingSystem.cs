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
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Containers;
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
using Content.Shared.SS220.ItemToggle;

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

        SubscribeLocalEvent<BlockingUserComponent, BeforeThrowEvent>(OnBeforeThrow);

        //SubscribeLocalEvent<BlockingComponent, UseInHandEvent>(OnUseInHand);
        //SS220 shield rework end

        SubscribeLocalEvent<BlockingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<BlockingComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<BlockingComponent, DroppedEvent>(OnDrop);

        // SS220 equip shield on back begin
        SubscribeLocalEvent<BlockingComponent, GotEquippedEvent>(OnGotEquip);
        SubscribeLocalEvent<BlockingComponent, GotUnequippedEvent>(OnGotUnequipped);
        // SS220 equip shield on back end

        SubscribeLocalEvent<BlockingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<BlockingComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<BlockingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<BlockingComponent, GetVerbsEvent<ExamineVerb>>(OnVerbExamine);
        SubscribeLocalEvent<BlockingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<BlockingComponent, ItemToggledEvent>(OnToggleItem);

        //ss220 fix drop shields start
        SubscribeLocalEvent<BlockingComponent, ThrowItemAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<BlockingComponent, ContainerGettingRemovedAttemptEvent>(OnDropAttempt);
        //ss220 fix drop shields end
    }

    //SS220 shield rework begin
    private void OnBeforeThrow(Entity<BlockingUserComponent> ent, ref BeforeThrowEvent args)
    {
        if (ent.Comp.IsBlocking) { args.Cancelled=true; }
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
            if(!TryGetNetEntity(item, out var netEnt)) { return; }
            if (TryComp<ItemToggleBlockingDamageComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                {
                    continue;
                }
            }
            if (ent.Comp.IsBlocking)
            {
                if (_random.Prob(shield.ActiveMeleeBlockProb))
                {
                    args.Cancelled = true;
                    args.blocker = netEnt;
                    return;
                }
            }
            else
            {
                if (_random.Prob(shield.MeleeBlockProb))
                {
                    args.Cancelled = true;
                    args.blocker = netEnt;
                    return;
                }
            }
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
                    return true;
                }
            }
            else
            {
                if (_random.Prob(shield.RangeBlockProb))
                {
                    _damageable.TryChangeDamage(shield.Owner, damage);
                    return true;
                }
            }
        }
        return false;
    }

    public void OnUseInHand(Entity<BlockingComponent> ent,ref UseInHandEvent args)
    {
        if(TryComp<BlockingUserComponent>(ent.Comp.Owner,out var user))
        {
            if(user.IsBlocking)
            {
                StopBlocking(ent.Owner, ent.Comp, user, args.User);
                return;
            }
            StartBlocking(ent.Owner, ent.Comp, user, args.User);
        }
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
        var action = ent.Comp.BlockingToggleActionEntity;

        if (action == null || !TryComp<ActionComponent>(action.Value, out var actionComponent))
            return false;

        return _gameTiming.CurTime <= actionComponent.Cooldown?.End;
    }
    //ss220 fix drop shields end

    //ss220 raise shield activated fix start
    private void OnToggleItem(Entity<BlockingComponent> ent, ref ItemToggledEvent args)
    {
        if (ent.Comp.User == null)
            return;

        if (!args.Activated && TryComp<BlockingUserComponent>(ent.Comp.User,out var compUser))
        {
            StopBlocking(ent.Owner, ent.Comp, compUser, ent.Comp.User.Value);
        }
    }
    //ss220 raise shield activated fix end

    private void OnMapInit(EntityUid uid, BlockingComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
        Dirty(uid, component);
    }

    private void OnEquip(EntityUid uid, BlockingComponent component, GotEquippedHandEvent args)
    {
        component.User = args.User;
        Dirty(uid, component);

        //To make sure that this bodytype doesn't get set as anything but the original)
        var userComp = EnsureComp<BlockingUserComponent>(args.User);
        userComp.BlockingItemsShields.Add(uid);
    }

    // SS220 equip shield on back begin
    private void OnGotEquip(EntityUid uid, BlockingComponent component, GotEquippedEvent args)
    {

        if (!component.AvaliableSlots.ContainsKey(args.SlotFlags))
            return;

        component.User = args.Equipee;
        Dirty(uid, component);

        if (TryComp<PhysicsComponent>(args.Equipee, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static)
        {
            var userComp = EnsureComp<BlockingUserComponent>(args.Equipee);
            userComp.BlockingItemsShields.Add(uid);
            //userComp.OriginalBodyType = physicsComponent.BodyType;
        }
    }

    private void OnGotUnequipped(EntityUid uid, BlockingComponent component, GotUnequippedEvent args)
    {
        StopBlockingHelper(uid, component, args.Equipee);
    }
    // SS220 equip shield on back end

    private void OnUnequip(EntityUid uid, BlockingComponent component, GotUnequippedHandEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    private void OnDrop(EntityUid uid, BlockingComponent component, DroppedEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    private void OnGetActions(EntityUid uid, BlockingComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
    }

    private void OnToggleAction(EntityUid uid, BlockingComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var blockUserQuery = GetEntityQuery<BlockingUserComponent>();
        var blockQuery = GetEntityQuery<BlockingComponent>();
        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(args.Performer, out var hands))
            return;

        if (!blockUserQuery.TryGetComponent(args.Performer, out var blockUser))
            return;

        var shields = _handsSystem.EnumerateHeld((args.Performer, hands)).ToArray();

        foreach (var shield in shields)
        {
            if (shield == uid)
                continue;

            if (blockQuery.TryGetComponent(shield, out var otherBlockComp) && otherBlockComp.IsBlocking)
            {
                CantBlockError(args.Performer);
                return;
            }
        }

        if (blockUser.IsBlocking)
            StopBlocking(uid, component, blockUser, args.Performer);
        else
            StartBlocking(uid, component, blockUser, args.Performer);

        args.Handled = true;
    }

    private void OnShutdown(EntityUid uid, BlockingComponent component, ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (component.User != null)
        {
            _actionsSystem.RemoveProvidedActions(component.User.Value, uid);
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
    public bool StartBlocking(EntityUid item, BlockingComponent component, BlockingUserComponent compUser, EntityUid user)
    {
        if (compUser.IsBlocking)
            return false;

        //var xform = Transform(user);

        var shieldName = Name(item);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("action-popup-blocking-user", ("shield", shieldName));
        var msgOther = Loc.GetString("action-popup-blocking-other", ("blockerName", blockerName), ("shield", shieldName));

        //Don't allow someone to block if they're not parented to a grid
        //if (xform.GridUid != xform.ParentUid)
        //{
        //CantBlockError(user);
        //return false;
        //}

        // Don't allow someone to block if they're not holding the shield
        if (!_handsSystem.IsHolding(user, item, out _))
        {
            CantBlockError(user);
            return false;
        }

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
        _actionsSystem.SetToggled(component.BlockingToggleActionEntity, true);
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

        ActiveBlockingEvent ev = new ActiveBlockingEvent(true);
        RaiseLocalEvent(item,ev);

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
    public bool StopBlocking(EntityUid item, BlockingComponent component, BlockingUserComponent compUser, EntityUid user)
    {
        if (!compUser.IsBlocking)
            return false;
        //var xform = Transform(user);

        var shieldName = Name(item);

        var blockerName = Identity.Entity(user, EntityManager);
        var msgUser = Loc.GetString("action-popup-blocking-disabling-user", ("shield", shieldName));
        var msgOther = Loc.GetString("action-popup-blocking-disabling-other", ("blockerName", blockerName), ("shield", shieldName));

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

        ActiveBlockingEvent ev=new ActiveBlockingEvent(false);
        RaiseLocalEvent(item,ev);

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
        if (component.IsBlocking)
            StopBlocking(uid, component, component1, user);

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
                RemComp<BlockingUserComponent>(user);
            }
        }
    }

    private void OnVerbExamine(EntityUid uid, BlockingComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        //if (!args.CanInteract || !args.CanAccess)
        //return;

        //var fraction = component.IsBlocking ? component.ActiveBlockFraction : component.PassiveBlockFraction;
        //var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;

        // var msg = new FormattedMessage();
        //msg.AddMarkupOrThrow(Loc.GetString("blocking-fraction", ("value", MathF.Round(fraction * 100, 1))));

        //AppendCoefficients(modifier, msg);

        //_examine.AddDetailedExamineVerb(args, component, msg,
        //    Loc.GetString("blocking-examinable-verb-text"),
        //    "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
        //    Loc.GetString("blocking-examinable-verb-message")
        //);
    }
}

[Serializable, NetSerializable]
public sealed class ActiveBlockingEvent(bool active) : EntityEventArgs
{
    public bool Active = active;
}
