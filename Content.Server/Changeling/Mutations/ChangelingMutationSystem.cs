// SS220 Changeling
using System.Linq;
using Content.Server.Changeling.Components;
using Content.Server.Changeling.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Changeling.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Mutations;

/// <summary>
/// Authoritative implementation of combat and defensive changeling mutations.
/// The evolution store owns purchased action lifetime; this system owns chemical
/// costs and temporary body effects.
/// </summary>
public sealed partial class ChangelingMutationSystem : EntitySystem
{
    private const string ChitinRegenerationKey = "changeling-chitinous-armor";
    private static readonly FixedPoint2 ArmBladeDrain = FixedPoint2.New(0.75f);
    private static readonly FixedPoint2 StrainedMusclesDamageLimit = FixedPoint2.New(90);
    private static readonly TimeSpan ArmBladeDrainInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StrainedMusclesInterval = TimeSpan.FromSeconds(1);

    private static readonly EntProtoId StoreAction = "ActionChangelingStore";
    private static readonly EntProtoId ArmBladePrototype = "ChangelingArmBlade";
    private static readonly EntProtoId BoneShardPrototype = "ChangelingBoneShard";
    private static readonly EntProtoId ChitinousArmorVisualPrototype = "ChangelingChitinousArmorVisual";
    private static readonly EntProtoId ChitinousHelmetVisualPrototype = "ChangelingChitinousHelmetVisual";
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> CellularDamage = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> PoisonDamage = "Poison";
    private static readonly ProtoId<DamageTypePrototype> RadiationDamage = "Radiation";
    private const string StoredArmorOuterClothing = "changeling-stored-armor-outer";
    private const string StoredArmorHead = "changeling-stored-armor-head";
    private static readonly SoundPathSpecifier MutationFormSound = new("/Audio/_Goobstation/Changeling/Effects/armour_transform.ogg");
    private static readonly SoundPathSpecifier MutationRetractSound = new("/Audio/_Goobstation/Changeling/Effects/armour_strip.ogg");
    private static readonly SoundPathSpecifier MutationShriekSound = new("/Audio/Voice/Vox/shriek1.ogg");

    // Reused synchronous damage payloads for Update-driven effects. Values are reset before every call because
    // the damage pipeline may apply global modifiers to the supplied specifier in place.
    private readonly DamageSpecifier _epinephrineCrashDamage = new()
    {
        DamageDict = { { PoisonDamage, FixedPoint2.New(8) } },
    };
    private readonly DamageSpecifier _strainedMusclesDamage = new()
    {
        DamageDict = { { BluntDamage, FixedPoint2.New(1) } },
    };

    [Dependency] private readonly ChangelingResourceSystem _resources = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _identities = default!;
    [Dependency] private readonly ChangelingDevourSystem _devour = default!;
    [Dependency] private readonly ChangelingTransformSystem _transformAbility = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ActionGrantSystem _actionGrant = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingArmBladeActionEvent>(OnArmBlade);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingBoneShardActionEvent>(OnBoneShard);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingResonantShriekActionEvent>(OnResonantShriek);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingSwapFormsActionEvent>(OnSwapForms);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingLastResortActionEvent>(OnLastResort);
        SubscribeLocalEvent<ChangelingHeadslugComponent, ChangelingLayEggActionEvent>(OnLayEgg);
        SubscribeLocalEvent<ChangelingHeadslugComponent, ComponentShutdown>(OnHeadslugShutdown);
        SubscribeLocalEvent<ChangelingIncubatingEggComponent, ComponentShutdown>(OnIncubatingEggShutdown);

        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingBiodegradeActionEvent>(OnBiodegrade);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingEpinephrineActionEvent>(OnEpinephrine);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingFleshmendActionEvent>(OnFleshmend);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingOrganicShieldActionEvent>(OnOrganicShield);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingChitinousArmorActionEvent>(OnChitinousArmor);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingAnatomicPanaceaActionEvent>(OnAnatomicPanacea);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingStrainedMusclesActionEvent>(OnStrainedMuscles);

        SubscribeLocalEvent<ChangelingOrganicShieldComponent, BeforeDamageChangedEvent>(OnOrganicShieldDamaged);
        SubscribeLocalEvent<ChangelingMutationStateComponent, BeingGibbedEvent>(OnMutationStateGibbed);
        SubscribeLocalEvent<ChangelingMutationStateComponent, ComponentShutdown>(OnMutationStateShutdown);
        SubscribeLocalEvent<ChangelingMutationStateComponent, ChangelingResourceRemovedEvent>(OnResourceRemoved);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingEvolutionResetEvent>(OnEvolutionReset);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var stateQuery = EntityQueryEnumerator<ChangelingMutationStateComponent, ChangelingResourceComponent>();
        while (stateQuery.MoveNext(out var uid, out var state, out var resources))
        {
            UpdateArmBlade((uid, state, resources), now);
            UpdateEpinephrine((uid, state), now);
            UpdateStrainedMuscles((uid, state), now);
        }

        UpdateEggs(now);
    }

    private bool TrySpend(Entity<ChangelingResourceComponent> ent, float amount)
    {
        return float.IsFinite(amount) &&
               amount >= 0f &&
               _resources.TrySpendChemicals(ent.AsNullable(), FixedPoint2.New(amount));
    }

    private void PopupMutation(EntityUid uid, string message)
    {
        _popup.PopupEntity(Loc.GetString(message), uid, uid);
    }

    private void PlayMutationFormSound(EntityUid uid) => _audio.PlayPvs(MutationFormSound, uid);
    private void PlayMutationRetractSound(EntityUid uid) => _audio.PlayPvs(MutationRetractSound, uid);
    private void PlayMutationShriekSound(EntityUid uid) => _audio.PlayPvs(MutationShriekSound, uid);

    private bool StoreEquippedItem(EntityUid uid, string slot, string containerId)
    {
        var container = _containers.EnsureContainer<ContainerSlot>(uid, containerId);
        if (container.ContainedEntity != null)
            return false;
        if (!_inventory.TryGetSlotEntity(uid, slot, out _))
            return true;
        if (!_inventory.TryUnequip(uid, slot, out var item, silent: true, force: true))
            return false;
        if (_containers.Insert(item.Value, container))
            return true;

        _inventory.TryEquip(uid, item.Value, slot, silent: true, force: true);
        return false;
    }

    private bool RestoreStoredItem(EntityUid uid, string slot, string containerId)
    {
        var container = _containers.EnsureContainer<ContainerSlot>(uid, containerId);
        if (container.ContainedEntity is not { } item || !Exists(item))
            return true;

        if (_inventory.TryEquip(uid, item, slot, silent: true, force: true))
            return true;

        if (_hands.TryPickupAnyHand(uid, item, checkActionBlocker: false))
            return true;

        if (_containers.Remove(item, container, force: true))
        {
            _transform.DropNextTo(item, uid);
            return true;
        }

        Log.Error($"Failed to restore stored changeling item {ToPrettyString(item)} to {ToPrettyString(uid)}.");
        return false;
    }

    private void DropStoredItem(EntityUid uid, string containerId)
    {
        if (!_containers.TryGetContainer(uid, containerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return;
        }

        foreach (var item in container.ContainedEntities.ToArray())
        {
            if (!_containers.Remove(item, container, force: true))
            {
                Log.Error($"Failed to release stored changeling item {ToPrettyString(item)} from {ToPrettyString(uid)}.");
                continue;
            }

            _transform.DropNextTo(item, uid);
        }
    }

    private void RemoveEquippedVisual(EntityUid uid, string slot, EntityUid? visual)
    {
        if (visual is not { } item || TerminatingOrDeleted(item))
            return;

        if (_inventory.TryGetSlotEntity(uid, slot, out var equipped) &&
            equipped == item &&
            !_inventory.TryUnequip(uid, slot, out _, silent: true, force: true) &&
            _containers.TryGetContainingContainer(item, out var container))
        {
            _containers.Remove(item, container, force: true);
        }

        QueueDel(item);
    }

    private void UpdateArmBlade(
        Entity<ChangelingMutationStateComponent, ChangelingResourceComponent> ent,
        TimeSpan now)
    {
        if (ent.Comp1.ArmBlade is not { } blade)
            return;

        if (TerminatingOrDeleted(blade))
        {
            DeactivateArmBlade((ent.Owner, ent.Comp1));
            return;
        }

        if (ent.Comp1.NextArmBladeDrain > now)
            return;

        ent.Comp1.NextArmBladeDrain += ArmBladeDrainInterval;
        if (_resources.TrySpendChemicals((ent.Owner, ent.Comp2), ArmBladeDrain))
            return;

        DeactivateArmBlade((ent.Owner, ent.Comp1));
    }

    private void UpdateEpinephrine(Entity<ChangelingMutationStateComponent> ent, TimeSpan now)
    {
        if (ent.Comp.EpinephrineEndsAt is not { } endsAt || endsAt > now)
            return;

        ent.Comp.EpinephrineEndsAt = null;
        Dirty(ent);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);

        _epinephrineCrashDamage.DamageDict.Clear();
        _epinephrineCrashDamage.ArmourPiercing = FixedPoint2.Zero;
        _epinephrineCrashDamage.DamageDict.Add(PoisonDamage, FixedPoint2.New(8));
        _damageable.ChangeDamage(ent.Owner, _epinephrineCrashDamage, origin: ent.Owner);
    }

    private void UpdateStrainedMuscles(Entity<ChangelingMutationStateComponent> ent, TimeSpan now)
    {
        if (!ent.Comp.StrainedMusclesActive || ent.Comp.NextStrainedMusclesTick > now)
            return;

        ent.Comp.NextStrainedMusclesTick += StrainedMusclesInterval;
        if (!TryComp<PhysicsComponent>(ent, out var physics) || physics.LinearVelocity.LengthSquared() < 0.04f)
            return;

        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return;

        _stamina.TakeStaminaDamage(ent.Owner, 6f, stamina, source: ent.Owner, visual: false);
        if (stamina.StaminaDamage < stamina.CritThreshold ||
            !TryComp<DamageableComponent>(ent, out var damageable))
            return;

        if (_damageable.IsDamageAtLeast((ent.Owner, damageable), StrainedMusclesDamageLimit))
            return;

        _strainedMusclesDamage.DamageDict.Clear();
        _strainedMusclesDamage.ArmourPiercing = FixedPoint2.Zero;
        _strainedMusclesDamage.DamageDict.Add(BluntDamage, FixedPoint2.New(1));
        _damageable.ChangeDamage(ent.Owner, _strainedMusclesDamage, origin: ent.Owner);
    }

    private void OnEvolutionReset(Entity<ChangelingResourceComponent> ent, ref ChangelingEvolutionResetEvent args)
    {
        if (!TryComp<ChangelingMutationStateComponent>(ent, out var state))
            return;

        CleanupTemporaryEffects((ent.Owner, state), ent.Comp);
    }

    private void OnMutationStateGibbed(Entity<ChangelingMutationStateComponent> ent, ref BeingGibbedEvent args)
    {
        CleanupMutationStateForRemoval(ent, dropStoredItems: true);
    }

    private void OnMutationStateShutdown(Entity<ChangelingMutationStateComponent> ent, ref ComponentShutdown args)
    {
        CleanupMutationStateForRemoval(ent, TerminatingOrDeleted(ent.Owner));
    }

    private void OnResourceRemoved(
        Entity<ChangelingMutationStateComponent> ent,
        ref ChangelingResourceRemovedEvent args)
    {
        CleanupMutationStateForRemoval(ent, args.EntityTerminating);
    }

    private void CleanupMutationStateForRemoval(Entity<ChangelingMutationStateComponent> ent, bool dropStoredItems)
    {
        DeactivateArmBlade(ent);
        DeactivateOrganicShield(ent);

        if (ent.Comp.ChitinousArmorActive)
            _resources.RemoveChemicalRegenerationModifier(ent.Owner, ChitinRegenerationKey);

        ent.Comp.ChitinousArmorActive = false;
        ent.Comp.StrainedMusclesActive = false;
        ent.Comp.EpinephrineEndsAt = null;
        RemoveEquippedVisual(ent.Owner, "outerClothing", ent.Comp.ChitinousArmorVisual);
        RemoveEquippedVisual(ent.Owner, "head", ent.Comp.ChitinousHelmetVisual);
        ent.Comp.ChitinousArmorVisual = null;
        ent.Comp.ChitinousHelmetVisual = null;

        if (dropStoredItems)
        {
            DropStoredItem(ent.Owner, StoredArmorOuterClothing);
            DropStoredItem(ent.Owner, StoredArmorHead);
        }
        else
        {
            RestoreStoredItem(ent.Owner, "outerClothing", StoredArmorOuterClothing);
            RestoreStoredItem(ent.Owner, "head", StoredArmorHead);
        }

        if (!TerminatingOrDeleted(ent.Owner))
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void CleanupTemporaryEffects(
        Entity<ChangelingMutationStateComponent> ent,
        ChangelingResourceComponent? resources = null)
    {
        DeactivateArmBlade(ent);
        DeactivateOrganicShield(ent);

        ent.Comp.StrainedMusclesActive = false;
        ent.Comp.EpinephrineEndsAt = null;
        if (ent.Comp.ChitinousArmorActive)
        {
            ent.Comp.ChitinousArmorActive = false;
            _resources.RemoveChemicalRegenerationModifier((ent.Owner, resources), ChitinRegenerationKey);
        }
        RemoveChitinousVisuals(ent.Owner, ent.Comp);

        Dirty(ent);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void TransferMutationActions(EntityUid from, EntityUid to)
    {
        if (!TryComp<StoreComponent>(from, out var store))
            return;

        // The store is authoritative for which action entities are mutations. This includes utility and
        // sting purchases as well as combat mutations, without accidentally moving species/job actions.
        foreach (var action in store.BoughtEntities.ToArray())
        {
            if (!Exists(action) || !HasComp<ActionComponent>(action))
                continue;

            _actions.SetToggled(action, false);
            _actionContainer.TransferActionWithNewAttached(action, to, to);
        }
    }

    private void TransferStoreAction(EntityUid source, EntityUid target)
    {
        _actionGrant.TryTransferGrantedAction(source, target, StoreAction, out _);
    }

    private void MoveStore(EntityUid source, EntityUid target)
    {
        if (!TryComp<StoreComponent>(source, out var oldStore))
            return;

        var targetStore = EnsureComp<StoreComponent>(target);
        targetStore.Name = oldStore.Name;
        targetStore.Categories = new HashSet<ProtoId<StoreCategoryPrototype>>(oldStore.Categories);
        targetStore.Balance = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(oldStore.Balance);
        targetStore.CurrencyWhitelist = new HashSet<ProtoId<CurrencyPrototype>>(oldStore.CurrencyWhitelist);
        targetStore.ExpectedFaction = oldStore.ExpectedFaction == null
            ? null
            : new HashSet<ProtoId<Content.Shared.NPC.Prototypes.NpcFactionPrototype>>(oldStore.ExpectedFaction);
        targetStore.AccountOwner = oldStore.AccountOwner;
        targetStore.FullListingsCatalog = oldStore.FullListingsCatalog;
        targetStore.LastAvailableListings = oldStore.LastAvailableListings;
        targetStore.BoughtEntities = new List<EntityUid>(oldStore.BoughtEntities);
        targetStore.BalanceSpent = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(oldStore.BalanceSpent);
        targetStore.RefundAllowed = oldStore.RefundAllowed;
        targetStore.OwnerOnly = oldStore.OwnerOnly;
        targetStore.StartingMap = Transform(target).MapUid;
        targetStore.CurrencyInsertTime = oldStore.CurrencyInsertTime;
        targetStore.UseDynamicPrices = oldStore.UseDynamicPrices;
        targetStore.BuySuccessSound = oldStore.BuySuccessSound;
        _store.RetargetBoughtEntities(targetStore, target);

        oldStore.BoughtEntities.Clear();
        RemComp<StoreComponent>(source);

        Dirty(target, targetStore);
    }

    private void MoveCoreAbilities(EntityUid source, EntityUid target)
    {
        if (TryComp<ChangelingDevourComponent>(source, out var devour))
        {
            if (!_devour.TryTransferTo((source, devour), target))
                Log.Error($"Failed to move devour ability from {ToPrettyString(source)} to {ToPrettyString(target)}.");
        }

        if (TryComp<ChangelingTransformComponent>(source, out var transform))
        {
            if (!_transformAbility.TryTransferTo((source, transform), target))
                Log.Error($"Failed to move transform ability from {ToPrettyString(source)} to {ToPrettyString(target)}.");
        }

        if (TryComp<ChangelingExtractDnaComponent>(source, out var extract))
        {
            AddComp(target, new ChangelingExtractDnaComponent
            {
                Action = extract.Action,
                ChemicalCost = extract.ChemicalCost,
            });
            RemComp<ChangelingExtractDnaComponent>(source);
        }
    }

    private bool TransferChangelingBody(
        Entity<ChangelingResourceComponent> source,
        EntityUid target,
        string? resultingGenome = null)
    {
        if (!CanTransferChangelingBody(source, target) ||
            !TryComp<ChangelingIdentityComponent>(source, out var identity))
            return false;

        // Body-bound toggles (armor, stealth, organic suit, etc.) are deliberately shut down. Learned
        // mutation actions and persistent DNA/resource state are moved below.
        var cleanup = new ChangelingEvolutionResetEvent(
            source.Comp.EvolutionPoints,
            source.Comp.EvolutionPoints);
        RaiseLocalEvent(source.Owner, ref cleanup);

        TransferMutationActions(source.Owner, target);
        TransferStoreAction(source.Owner, target);
        MoveStore(source.Owner, target);
        if (!_identities.TryTransferIdentity((source.Owner, identity), target))
            return false;
        if (TryComp<ChangelingIdentityComponent>(target, out var targetIdentity))
        {
            targetIdentity.CurrentGenome = resultingGenome;
            targetIdentity.CurrentIdentity = null;
            Dirty(target, targetIdentity);
        }
        MoveCoreAbilities(source.Owner, target);
        MoveMutationState(source.Owner, target);
        MoveResources(source, target);
        return true;
    }

    /// <summary>
    /// Validates every body-level ownership conflict before any component, action, store balance, or identity
    /// is moved. Transfer helpers below are intentionally non-failing after this preflight succeeds.
    /// </summary>
    private bool CanTransferChangelingBody(Entity<ChangelingResourceComponent> source, EntityUid target)
    {
        return source.Owner != target &&
               !TerminatingOrDeleted(source.Owner) &&
               !TerminatingOrDeleted(target) &&
               HasComp<ChangelingIdentityComponent>(source.Owner) &&
               !HasComp<ChangelingIdentityComponent>(target) &&
               !HasComp<ChangelingResourceComponent>(target) &&
               !HasComp<StoreComponent>(target) &&
               !HasComp<ChangelingDevourComponent>(target) &&
               !HasComp<ChangelingTransformComponent>(target) &&
               !HasComp<ChangelingExtractDnaComponent>(target) &&
               !HasComp<PolymorphedEntityComponent>(target) &&
               !HasComp<ChangelingMutationStateComponent>(target);
    }

    private void MoveResources(Entity<ChangelingResourceComponent> source, EntityUid target)
    {
        var targetResources = new ChangelingResourceComponent
        {
            ChemicalsAlert = source.Comp.ChemicalsAlert,
            Chemicals = source.Comp.Chemicals,
            MaxChemicals = source.Comp.MaxChemicals,
            EvolutionPoints = source.Comp.EvolutionPoints,
            MaxEvolutionPoints = source.Comp.MaxEvolutionPoints,
            ChemicalRegenerationAmount = source.Comp.ChemicalRegenerationAmount,
            ChemicalRegenerationInterval = source.Comp.ChemicalRegenerationInterval,
            NextChemicalRegeneration = source.Comp.NextChemicalRegeneration > _timing.CurTime
                ? source.Comp.NextChemicalRegeneration
                : _timing.CurTime + source.Comp.ChemicalRegenerationInterval,
            ChemicalRegenerationModifiers = new Dictionary<string, float>(source.Comp.ChemicalRegenerationModifiers),
            RegenerativeStasisAction = source.Comp.RegenerativeStasisAction,
            RegenerateAction = source.Comp.RegenerateAction,
            RegenerativeStasisChemicalCost = source.Comp.RegenerativeStasisChemicalCost,
            RegenerativeStasisDuration = source.Comp.RegenerativeStasisDuration,
            InRegenerativeStasis = false,
            CanRegenerateAt = null,
            RegenerationPermanentlyBlocked = false,
        };

        RemComp<ChangelingResourceComponent>(source.Owner);
        AddComp(target, targetResources);
    }

    private void MoveMutationState(EntityUid source, EntityUid target)
    {
        if (!TryComp<ChangelingMutationStateComponent>(source, out var oldState))
            return;

        var targetState = EnsureComp<ChangelingMutationStateComponent>(target);
        targetState.LastFleshmend = oldState.LastFleshmend;
        targetState.EpinephrineEndsAt = oldState.EpinephrineEndsAt;
        RemComp<ChangelingMutationStateComponent>(source);
        Dirty(target, targetState);
        _movement.RefreshMovementSpeedModifiers(target);
    }
}
