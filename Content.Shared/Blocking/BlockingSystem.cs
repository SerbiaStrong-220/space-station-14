using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
//using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;//SS220 shield rework
using Content.Shared.Projectiles;//SS220 shield rework
using Content.Shared.SS220.ItemToggle;//SS220 shield rework
//using Content.Shared.Item.ItemToggle.Components;
//using Content.Shared.Maps;
//using Content.Shared.Mobs.Components;
//using Content.Shared.Physics;
//using Content.Shared.Popups;
//using Content.Shared.Toggleable;
//using Content.Shared.Verbs;
//using Robust.Shared.Physics;
//using Robust.Shared.Physics.Components;
//using Robust.Shared.Physics.Systems;
//using Content.Shared.Weapons.Reflect;
using Content.Shared.SS220.Weapons.Melee.Events;//SS220 shield rework
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;//SS220 shield rework
using Robust.Shared.Containers;
using Robust.Shared.Network;//SS220 shield rework
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;//SS220 shield rework
using Robust.Shared.Serialization;//SS220 shield rework
using Robust.Shared.Timing;
using Robust.Shared.Utility;//SS220 shield rework
using System.Linq;

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
    [Dependency] private readonly DamageableSystem _damageable = default!;//SS220 shield rework
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    //[Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IRobustRandom _random = default!;//SS220 shield rework
    //[Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly INetManager _net = default!;//SS220 shield rework

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

        //SubscribeLocalEvent<BlockingComponent, ToggleActionEvent>(OnToggleAction);
        //SubscribeLocalEvent<BlockingComponent, GetItemActionsEvent>(OnGetActions);

        //SubscribeLocalEvent<BlockingComponent, ItemToggledEvent>(OnToggleItem);

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

    //SS220 shield rework begin
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
        args.Cancelled = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
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
                    args.Cancelled = true;
                    args.blocker = netEnt;
                    ChangeSeed(ent);
                    _audio.PlayPvs(shield.BlockSound, (EntityUid)item);
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
                    _audio.PlayPvs(shield.BlockSound, (EntityUid)item);
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
                if (!toggleComp.IsToggled)
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
        }
        ChangeSeed((comp.Owner, comp));
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
        //var action = ent.Comp.BlockingToggleActionEntity;
        //SS220 shield rework bagin
        if (!TryComp<BlockingUserComponent>(ent.Comp.User, out var userComp))
        {
            return false;
        }
        var action = userComp.BlockingToggleActionEntity;

        if (action == null || !TryComp<ActionComponent>(action.Value, out var actionComponent))
            return false;

        return _gameTiming.CurTime <= actionComponent.Cooldown?.End;
        //SS220 shield rework end
    }
    //ss220 fix drop shields end

    //private void OnMapInit(EntityUid uid, BlockingComponent component, MapInitEvent args)
    //{
    //    _actionContainer.EnsureAction(uid, ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
    //    Dirty(uid, component);
    //}

    private void OnEquip(EntityUid uid, BlockingComponent component, GotEquippedHandEvent args)
    {
        component.User = args.User;
        Dirty(uid, component);

        //To make sure that this bodytype doesn't get set as anything but the original
        //if (TryComp<PhysicsComponent>(args.User, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static && !HasComp<BlockingUserComponent>(args.User))
        //{
        //    var userComp = EnsureComp<BlockingUserComponent>(args.User);
        //    userComp.BlockingItem = uid;
        //    userComp.OriginalBodyType = physicsComponent.BodyType;
        //}

        //SS220 shield rework begin
        var userComp = EnsureComp<BlockingUserComponent>(args.User);
        userComp.BlockingItemsShields.Add(uid);
        _actionsSystem.AddAction(args.User, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.User);
        Dirty(args.User, userComp);
        //SS220 shield rework end
    }

    // SS220 equip shield on back begin
    private void OnGotEquip(EntityUid uid, BlockingComponent component, GotEquippedEvent args)
    {

        if (!component.AvaliableSlots.ContainsKey(args.SlotFlags))
            return;

        component.User = args.Equipee;
        Dirty(uid, component);

        //if (TryComp<PhysicsComponent>(args.Equipee, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static)
        //{
        //    var userComp = EnsureComp<BlockingUserComponent>(args.Equipee);
        //    userComp.BlockingItem = uid;
        //    userComp.OriginalBodyType = physicsComponent.BodyType;
        //}

        //SS220 shield rework begin
        var userComp = EnsureComp<BlockingUserComponent>(args.Equipee);
        _actionsSystem.AddAction(args.Equipee, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.Equipee);
        userComp.BlockingItemsShields.Add(uid);
        Dirty(args.Equipee, userComp);
        //SS220 shield rework end
    }

    private void OnGotUnequipped(EntityUid uid, BlockingComponent component, GotUnequippedEvent args)
    {
        StopBlockingHelper(uid, component, args.Equipee);
    }
    // SS220 equip shield on back end

    private void OnUnequip(EntityUid uid, BlockingComponent component, GotUnequippedHandEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
        //var userComp = EnsureComp<BlockingUserComponent>(args.User);//SS220 shield rework
    }

    private void OnDrop(EntityUid uid, BlockingComponent component, DroppedEvent args)
    {
        StopBlockingHelper(uid, component, args.User);
    }

    //private void OnGetActions(EntityUid uid, BlockingComponent component, GetItemActionsEvent args)
    //{
    //    args.AddAction(ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
    //}

    //private void OnToggleAction(EntityUid uid, BlockingComponent component, ToggleActionEvent args)
    //{
    //    if (args.Handled)
    //        return;

    //    var blockQuery = GetEntityQuery<BlockingComponent>();
    //    var handQuery = GetEntityQuery<HandsComponent>();

    //    if (!handQuery.TryGetComponent(args.Performer, out var hands))
    //        return;

    //    var shields = _handsSystem.EnumerateHeld((args.Performer, hands)).ToArray();

    //    foreach (var shield in shields)
    //    {
    //        if (shield == uid)
    //            continue;

    //        if (blockQuery.TryGetComponent(shield, out var otherBlockComp) && otherBlockComp.IsBlocking)
    //        {
    //            CantBlockError(args.Performer);
    //            return;
    //       }
    //   }

    //    if (component.IsBlocking)
    //        StopBlocking(uid, component, args.Performer);
    //    else
    //        StartBlocking(uid, component, args.Performer);

    //    args.Handled = true;
    //}

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
    // public bool StartBlocking(EntityUid item, BlockingComponent component, EntityUid user)
    public bool StartBlocking(BlockingUserComponent compUser, EntityUid user)//SS220 shield rework
    {
        //if (component.IsBlocking)
        if (compUser.IsBlocking)//SS220 shield rework
            return false;

        //var xform = Transform(user);

        //var shieldName = Name(item);

        var blockerName = Identity.Entity(user, EntityManager);
        //var msgUser = Loc.GetString("action-popup-blocking-user", ("shield", shieldName));
        //var msgOther = Loc.GetString("action-popup-blocking-other", ("blockerName", blockerName), ("shield", shieldName));
        var msgUser = Loc.GetString("actively-blocking-attack");//SS220 shield rework
        var msgOther = Loc.GetString("actively-blocking-others", ("blockerName", blockerName));//SS220 shield rework

        //Don't allow someone to block if they're not parented to a grid
        //if (xform.GridUid != xform.ParentUid)
        //{
        //CantBlockError(user);
        //return false;
        //}

        // Don't allow someone to block if they're not holding the shield
        //if (!_handsSystem.IsHolding(user, item, out _))
        //{
        //    CantBlockError(user);
        //    return false;
        //}

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
        //_actionsSystem.SetToggled(component.BlockingToggleActionEntity, true);
        _actionsSystem.SetToggled(compUser.BlockingToggleActionEntity, true);//SS220 shield rework
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
        //SS220 shield rework begin
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
            RaiseLocalEvent((EntityUid)shield, ev);
        }
        //SS220 shield rework end
        //component.IsBlocking = true;
        //Dirty(item, component);
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
    //public bool StopBlocking(EntityUid item, BlockingComponent component, EntityUid user)
    public bool StopBlocking(BlockingUserComponent compUser, EntityUid user)
    {
        //if (!component.IsBlocking)
        if (!compUser.IsBlocking)
            return false;
        //var xform = Transform(user);

        //var shieldName = Name(item);

        var blockerName = Identity.Entity(user, EntityManager);
        //var msgUser = Loc.GetString("action-popup-blocking-disabling-user", ("shield", shieldName));
        //var msgOther = Loc.GetString("action-popup-blocking-disabling-other", ("blockerName", blockerName), ("shield", shieldName));
        var msgUser = Loc.GetString("actively-blocking-stop");//SS220 shield rework
        var msgOther = Loc.GetString("actively-blocking-stop-others", ("blockerName", blockerName));//SS220 shield rework
        _popupSystem.PopupPredicted(msgUser, msgOther, user, user);//SS220 shield rework
        _actionsSystem.SetToggled(compUser.BlockingToggleActionEntity, false);//SS220 shield rework

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
        //SS220 shield rework begin
        compUser.IsBlocking = false;
        Dirty(user, compUser);
        foreach (var shield in compUser.BlockingItemsShields)
        {
            if (shield == null) { continue; }
            ActiveBlockingEvent ev = new ActiveBlockingEvent(false);
            RaiseLocalEvent((EntityUid)shield, ev);
        }
        //SS220 shield rework end
        //component.IsBlocking = false;
        //Dirty(item, component);

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
        //if (component.IsBlocking)
        //    StopBlocking(uid, component, user);
        var userQuery = GetEntityQuery<BlockingUserComponent>();
        //SS220 shield rework begin
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
            if (HasComp<BlockingComponent>(shield) && userQuery.TryGetComponent(user, out var blockingUserComponent))
            {
                //blockingUserComponent.BlockingItem = shield;
                return;
            }
        }
        //RemComp<BlockingUserComponent>(user);
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
        //SS220 shield rework end
    }

    //private void OnVerbExamine(EntityUid uid, BlockingComponent component, GetVerbsEvent<ExamineVerb> args)
    //{
    //    if (!args.CanInteract || !args.CanAccess)
    //        return;

    //    var fraction = component.IsBlocking ? component.ActiveBlockFraction : component.PassiveBlockFraction;
    //    var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;

    //    var msg = new FormattedMessage();
    //    msg.AddMarkupOrThrow(Loc.GetString("blocking-fraction", ("value", MathF.Round(fraction * 100, 1))));

    //    AppendCoefficients(modifier, msg);

    //    _examine.AddDetailedExamineVerb(args, component, msg,
    //        Loc.GetString("blocking-examinable-verb-text"),
    //        "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
    //        Loc.GetString("blocking-examinable-verb-message")
    //    );
    //}

    //private void AppendCoefficients(DamageModifierSet modifiers, FormattedMessage msg)
    //{
    //    foreach (var coefficient in modifiers.Coefficients)
    //    {
    //        msg.PushNewline();
    //        msg.AddMarkupOrThrow(Robust.Shared.Localization.Loc.GetString("blocking-coefficient-value",
    //            ("type", coefficient.Key),
    //            ("value", MathF.Round(coefficient.Value * 100, 1))
    //        ));
    //    }

    //    foreach (var flat in modifiers.FlatReduction)
    //    {
    //        msg.PushNewline();
    //        msg.AddMarkupOrThrow(Robust.Shared.Localization.Loc.GetString("blocking-reduction-value",
    //            ("type", flat.Key),
    //            ("value", flat.Value)
    //        ));
    //    }
    //}
}
//SS220 shield rework begin
[Serializable, NetSerializable]
public sealed class ActiveBlockingEvent(bool active) : EntityEventArgs
{
    public bool Active = active;
}
//SS220 shield rework end
