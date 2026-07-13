// SS220 Changeling
using System.Linq;
using Content.Server.Changeling.Components;
using Content.Server.Cuffs;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Drunk;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;
using Content.Shared.StatusEffectNew;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Temperature.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Server.Changeling.Mutations;

public sealed partial class ChangelingMutationSystem
{
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPathologySystem _pathology = default!;

    private void OnBiodegrade(Entity<ChangelingResourceComponent> ent, ref ChangelingBiodegradeActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        TryComp<CuffableComponent>(ent, out var cuffable);
        var parent = Transform(ent).ParentUid;
        var trapped = TryComp<EntityStorageComponent>(parent, out var storage);
        var hasCuffs = cuffable?.Container.ContainedEntities.Count > 0;

        if (!hasCuffs && !trapped || !TrySpend(ent, args.ChemicalCost))
            return;

        args.Handled = true;
        PopupMutation(ent.Owner, "changeling-biodegrade-used");
        if (cuffable != null)
        {
            foreach (var cuff in cuffable.Container.ContainedEntities.ToArray())
            {
                if (TryComp<HandcuffComponent>(cuff, out var handcuff))
                    _cuffable.Uncuff(ent.Owner, null, cuff, cuffable, handcuff);
            }
        }

        if (!trapped || storage == null)
            return;

        if (TryComp<WeldableComponent>(parent, out var weldable) && weldable.IsWelded)
            _weldable.SetWeldedState(parent, false, weldable);

        _entityStorage.OpenStorage(parent, storage);
    }

    private void OnEpinephrine(Entity<ChangelingResourceComponent> ent, ref ChangelingEpinephrineActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            args.BoostDuration < TimeSpan.Zero ||
            !TrySpend(ent, args.ChemicalCost))
            return;

        args.Handled = true;
        _stun.TryUnstun(ent.Owner);
        _stun.ForceStandUp(ent.Owner);
        if (TryComp<Content.Shared.Damage.Components.StaminaComponent>(ent, out var stamina))
            _stamina.TakeStaminaDamage(ent.Owner, -stamina.StaminaDamage, stamina, source: ent.Owner, visual: false);

        var state = EnsureComp<ChangelingMutationStateComponent>(ent);
        state.EpinephrineEndsAt = _timing.CurTime + args.BoostDuration;
        Dirty(ent.Owner, state);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
        PopupMutation(ent.Owner, "changeling-epinephrine-used");
    }

    private void OnFleshmend(Entity<ChangelingResourceComponent> ent, ref ChangelingFleshmendActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !float.IsFinite(args.Healing) ||
            args.Healing < 0f ||
            args.RepeatPenaltyWindow < TimeSpan.Zero ||
            !TrySpend(ent, args.ChemicalCost))
            return;

        args.Handled = true;
        var state = EnsureComp<ChangelingMutationStateComponent>(ent);
        var modifier = 1f;
        if (_timing.CurTime - state.LastFleshmend < args.RepeatPenaltyWindow)
            modifier *= 0.35f;
        if (TryComp<TemperatureComponent>(ent, out var temperature) && temperature.CurrentTemperature < 280f)
            modifier *= 0.5f;

        state.LastFleshmend = _timing.CurTime;
        Dirty(ent.Owner, state);
        _damageable.HealEvenly(ent.Owner, FixedPoint2.New(-args.Healing * modifier), origin: ent.Owner);
        PopupMutation(ent.Owner, "changeling-fleshmend-used");
    }

    private void OnOrganicShield(Entity<ChangelingResourceComponent> ent,
        ref ChangelingOrganicShieldActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureComp<ChangelingMutationStateComponent>(ent);
        if (state.OrganicShield is { } activeShield && !TerminatingOrDeleted(activeShield))
        {
            args.Handled = true;
            // DeactivateOrganicShield updates the action state explicitly because shield breakage also calls it.
            // Do not invert that state a second time in SharedActionsSystem.
            args.Toggle = false;
            DeactivateOrganicShield((ent.Owner, state));
            PlayMutationRetractSound(ent.Owner);
            PopupMutation(ent.Owner, "changeling-organic-shield-retracted");
            return;
        }

        if (!_hands.TryGetEmptyHand(ent.Owner, out var hand) || !TrySpend(ent, args.ChemicalCost))
            return;

        var shield = Spawn(args.ShieldPrototype, Transform(ent).Coordinates);
        if (!_hands.TryPickup(ent.Owner, shield, hand, checkActionBlocker: false))
        {
            QueueDel(shield);
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(args.ChemicalCost));
            return;
        }

        args.Handled = true;
        args.Toggle = true;
        state.OrganicShield = shield;
        state.OrganicShieldAction = args.Action;
        var shieldComp = EnsureComp<ChangelingOrganicShieldComponent>(shield);
        shieldComp.ChangelingOwner = ent.Owner;
        Dirty(shield, shieldComp);
        Dirty(ent.Owner, state);
        PlayMutationFormSound(ent.Owner);
        PopupMutation(ent.Owner, "changeling-organic-shield-formed");
    }

    private void OnOrganicShieldDamaged(Entity<ChangelingOrganicShieldComponent> ent,
        ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        args.Cancelled = true;
        ent.Comp.BlocksRemaining--;
        Dirty(ent);
        if (ent.Comp.BlocksRemaining > 0)
            return;

        if (ent.Comp.ChangelingOwner is { } owner && TryComp<ChangelingMutationStateComponent>(owner, out var state))
        {
            DeactivateOrganicShield((owner, state));
            PlayMutationRetractSound(owner);
            PopupMutation(owner, "changeling-organic-shield-broken");
        }
        else
            QueueDel(ent.Owner);
    }

    private void DeactivateOrganicShield(Entity<ChangelingMutationStateComponent> ent)
    {
        if (ent.Comp.OrganicShield is { } shield && !TerminatingOrDeleted(shield))
            QueueDel(shield);

        if (ent.Comp.OrganicShieldAction is { } action)
            _actions.SetToggled(action, false);

        ent.Comp.OrganicShield = null;
        ent.Comp.OrganicShieldAction = null;
        Dirty(ent);
    }

    private void OnChitinousArmor(Entity<ChangelingResourceComponent> ent,
        ref ChangelingChitinousArmorActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureComp<ChangelingMutationStateComponent>(ent);
        if (!state.ChitinousArmorActive)
        {
            if (HasComp<ChangelingOrganicSpaceSuitComponent>(ent.Owner))
            {
                PopupMutation(ent.Owner, "changeling-armor-conflict");
                return;
            }

            if (!TrySpend(ent, args.ChemicalCost))
                return;

            if (!TryEquipChitinousVisuals(ent.Owner, state))
            {
                _resources.AddChemicals(ent.Owner, FixedPoint2.New(args.ChemicalCost));
                PopupMutation(ent.Owner, "changeling-armor-visual-failed");
                return;
            }
        }

        args.Handled = true;
        args.Toggle = true;
        state.ChitinousArmorActive = !state.ChitinousArmorActive;
        if (state.ChitinousArmorActive)
        {
            _resources.SetChemicalRegenerationModifier(ent.AsNullable(), ChitinRegenerationKey, 0.5f);
            PlayMutationFormSound(ent.Owner);
        }
        else
        {
            _resources.RemoveChemicalRegenerationModifier(ent.AsNullable(), ChitinRegenerationKey);
            RemoveChitinousVisuals(ent.Owner, state);
            PlayMutationRetractSound(ent.Owner);
        }

        Dirty(ent.Owner, state);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
        PopupMutation(ent.Owner, state.ChitinousArmorActive ? "changeling-chitinous-armor-formed" : "changeling-chitinous-armor-retracted");
    }

    private bool TryEquipChitinousVisuals(EntityUid uid, ChangelingMutationStateComponent state)
    {
        if (!StoreEquippedItem(uid, "outerClothing", StoredArmorOuterClothing) ||
            !StoreEquippedItem(uid, "head", StoredArmorHead))
        {
            RestoreStoredItem(uid, "outerClothing", StoredArmorOuterClothing);
            RestoreStoredItem(uid, "head", StoredArmorHead);
            return false;
        }

        var coordinates = Transform(uid).Coordinates;
        var armor = Spawn(ChitinousArmorVisualPrototype, coordinates);
        if (!_inventory.TryEquip(uid, armor, "outerClothing", silent: true, force: true))
        {
            QueueDel(armor);
            RestoreStoredItem(uid, "outerClothing", StoredArmorOuterClothing);
            RestoreStoredItem(uid, "head", StoredArmorHead);
            return false;
        }

        var helmet = Spawn(ChitinousHelmetVisualPrototype, coordinates);
        if (!_inventory.TryEquip(uid, helmet, "head", silent: true, force: true))
        {
            QueueDel(helmet);
            RemoveEquippedVisual(uid, "outerClothing", armor);
            RestoreStoredItem(uid, "outerClothing", StoredArmorOuterClothing);
            RestoreStoredItem(uid, "head", StoredArmorHead);
            return false;
        }

        state.ChitinousArmorVisual = armor;
        state.ChitinousHelmetVisual = helmet;
        return true;
    }

    private void RemoveChitinousVisuals(EntityUid uid, ChangelingMutationStateComponent state)
    {
        RemoveEquippedVisual(uid, "outerClothing", state.ChitinousArmorVisual);
        RemoveEquippedVisual(uid, "head", state.ChitinousHelmetVisual);

        state.ChitinousArmorVisual = null;
        state.ChitinousHelmetVisual = null;
        RestoreStoredItem(uid, "outerClothing", StoredArmorOuterClothing);
        RestoreStoredItem(uid, "head", StoredArmorHead);
    }

    private void OnAnatomicPanacea(Entity<ChangelingResourceComponent> ent,
        ref ChangelingAnatomicPanaceaActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !TrySpend(ent, args.ChemicalCost))
            return;

        args.Handled = true;
        _damageable.ChangeDamage(ent.Owner,
            new DamageSpecifier
            {
                DamageDict =
                {
                    { PoisonDamage, FixedPoint2.New(-1000) },
                    { RadiationDamage, FixedPoint2.New(-1000) },
                    { CellularDamage, FixedPoint2.New(-1000) },
                },
            },
            ignoreResistances: true,
            interruptsDoAfters: false,
            origin: ent.Owner);

        _statusEffects.TryRemoveStatusEffect(ent.Owner, SharedDrunkSystem.Drunk);
        if (TryComp<PathologyHolderComponent>(ent, out var holder))
        {
            foreach (var pathology in holder.ActivePathologies.Keys.ToArray())
                _pathology.TryRemovePathology((ent.Owner, holder), pathology, checkStacks: false);
        }
        PopupMutation(ent.Owner, "changeling-anatomic-panacea-used");
    }

    private void OnStrainedMuscles(Entity<ChangelingResourceComponent> ent,
        ref ChangelingStrainedMusclesActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;
        args.Toggle = true;
        var state = EnsureComp<ChangelingMutationStateComponent>(ent);
        state.StrainedMusclesActive = !state.StrainedMusclesActive;
        state.NextStrainedMusclesTick = _timing.CurTime + StrainedMusclesInterval;
        Dirty(ent.Owner, state);
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
        PopupMutation(ent.Owner, state.StrainedMusclesActive ? "changeling-strained-muscles-enabled" : "changeling-strained-muscles-disabled");
    }
}
