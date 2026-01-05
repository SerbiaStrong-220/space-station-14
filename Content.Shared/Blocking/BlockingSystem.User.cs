using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.Components;//SS220 shield rework
using Content.Shared.Inventory;
using Content.Shared.Toggleable;//SS220 shield rework
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    //[Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!; // SS220 equip shield on back
    private void InitializeUser()
    {
        //SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);
        //SubscribeLocalEvent<BlockingComponent, DamageModifyEvent>(OnDamageModified);

        //SubscribeLocalEvent<BlockingUserComponent, EntParentChangedMessage>(OnParentChanged);
        //SubscribeLocalEvent<BlockingUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BlockingUserComponent, MapInitEvent>(OnMapInit);//SS220 shield rework
        SubscribeLocalEvent<BlockingUserComponent, ToggleActionEvent>(OnToggleAction);//SS220 shield rework
        //SubscribeLocalEvent<BlockingUserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        //SubscribeLocalEvent<BlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    //private void OnParentChanged(EntityUid uid, BlockingUserComponent component, ref EntParentChangedMessage args)
    //{
    //    UserStopBlocking(uid, component);
    //}

    //private void OnInsertAttempt(EntityUid uid, BlockingUserComponent component, ContainerGettingInsertedAttemptEvent args)
    //{
    //    UserStopBlocking(uid, component);
    //}

    //private void OnAnchorChanged(EntityUid uid, BlockingUserComponent component, ref AnchorStateChangedEvent args)
    //{
    //    if (args.Anchored)
    //        return;
    //
    //    UserStopBlocking(uid, component);
    //}

    //SS220 shield rework begin
    private void OnMapInit(EntityUid uid, BlockingUserComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.BlockingToggleActionEntity, component.BlockingToggleAction);
        Dirty(uid, component);
    }

    private void OnToggleAction(EntityUid uid, BlockingUserComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        var handQuery = GetEntityQuery<HandsComponent>();

        if (!handQuery.TryGetComponent(args.Performer, out var hands))
            return;

        if (component.IsBlocking)
            StopBlocking(component, args.Performer);
        else
            StartBlocking(component, args.Performer);
        Dirty(uid, component);
        args.Handled = true;
    }
    //SS220 shield rework end

    //private void OnUserDamageModified(EntityUid uid, BlockingUserComponent component, DamageModifyEvent args)
    //{
    //if (TryComp<BlockingComponent>(component.BlockingItem, out var blocking))
    //{
    //    if (args.Damage.GetTotal() <= 0)
    //       return;
    //
    //    var blockFraction = blocking.IsBlocking ? blocking.ActiveBlockFraction : blocking.PassiveBlockFraction;
    //
    // A shield should only block damage it can itself absorb. To determine that we need the Damageable component on it.
    //       if (!TryComp<DamageableComponent>(component.BlockingItem, out var dmgComp))
    //             return;

    // SS220 equip shield on back begin
    //         if (_inventory.TryGetContainingSlot(component.BlockingItem.Value, out var slotDefinition) && blocking.AvaliableSlots.TryGetValue(slotDefinition.SlotFlags, out var coef))
    //         {
    //             blockFraction *= coef;
    //         }
    //        // SS220 equip shield on back end

    //       blockFraction = Math.Clamp(blockFraction, 0, 1);
    //       _damageable.TryChangeDamage(component.BlockingItem, blockFraction * args.OriginalDamage);
    //
    //       var modify = new DamageModifierSet();
    //       foreach (var key in dmgComp.Damage.DamageDict.Keys)
    //        {
    //          modify.Coefficients.TryAdd(key, 1 - blockFraction);
    //      }

    //       args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modify);
    //
    //       if (blocking.IsBlocking && !args.Damage.Equals(args.OriginalDamage))
    //       {
    //           _audio.PlayPvs(blocking.BlockSound, uid);
    //        }
    //   }
    //   }

    //private void OnDamageModified(EntityUid uid, BlockingComponent component, DamageModifyEvent args)
    //{
    //    var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;
    //    if (modifier == null)
    //    {
    //        return;
    //    }
    //
    //    args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    //}

    //private void OnEntityTerminating(EntityUid uid, BlockingUserComponent component, ref EntityTerminatingEvent args)
    //{
    //    foreach (var shield in component.BlockingItemsShields)
    //    {
    //        if (!TryComp<BlockingComponent>(shield, out var blockingComponent))
    //            return;
    //
    //        StopBlockingHelper((EntityUid)shield, blockingComponent, uid);
    //    }
    //
    //}

    /// <summary>
    /// Check for the shield and has the user stop blocking
    /// Used where you'd like the user to stop blocking, but also don't want to remove the <see cref="BlockingUserComponent"/>
    /// </summary>
    /// <param name="uid">The user blocking</param>
    /// <param name="component">The <see cref="BlockingUserComponent"/></param>
    //private void UserStopBlocking(EntityUid uid, BlockingUserComponent component)
    //{
    //    foreach (var shield in component.BlockingItemsShields)
    //    {
    //       if (TryComp<BlockingComponent>(shield, out var blockComp) && blockComp.IsBlocking)
    //            StopBlocking(component, uid);
    //    }
    //}
}
