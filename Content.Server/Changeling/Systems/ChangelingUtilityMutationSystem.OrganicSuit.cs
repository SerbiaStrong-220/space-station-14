// SS220 Changeling
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Body.Systems;
using Content.Server.Changeling.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    private void InitializeOrganicSuit()
    {
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingOrganicSpaceSuitActionEvent>(OnOrganicSpaceSuit);
        SubscribeLocalEvent<ChangelingEnvironmentalProtectionComponent, GetTemperatureProtectionEvent>(OnEnvironmentalTemperatureProtection);
        SubscribeLocalEvent<ChangelingEnvironmentalProtectionComponent, BeforeRespirationCycleEvent>(OnBeforeRespirationCycle);
    }

    private void OnOrganicSpaceSuit(Entity<ChangelingResourceComponent> ent, ref ChangelingOrganicSpaceSuitActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureState(ent);
        if (!state.OrganicSpaceSuit)
        {
            if (TryComp<ChangelingMutationStateComponent>(ent, out var mutations) && mutations.ChitinousArmorActive)
            {
                _popup.PopupEntity(Loc.GetString("changeling-armor-conflict"), ent.Owner, ent.Owner);
                return;
            }

            if (!Spend(ent.Owner, 20))
                return;

            if (!TryEquipOrganicSuitVisuals(ent.Owner, state))
            {
                _resources.AddChemicals(ent.Owner, FixedPoint2.New(20));
                _popup.PopupEntity(Loc.GetString("changeling-armor-visual-failed"), ent.Owner, ent.Owner);
                return;
            }
        }

        state.OrganicSpaceSuit = !state.OrganicSpaceSuit;
        args.Handled = true;
        args.Toggle = true;

        if (state.OrganicSpaceSuit)
        {
            EnsureComp<ChangelingOrganicSpaceSuitComponent>(ent);
            UpdateEnvironmentalProtection(ent.Owner, state);
            _resources.SetChemicalRegenerationModifier(ent.Owner, OrganicSuitRegenKey, 0.5f);
            _audio.PlayPvs(MutationFormSound, ent.Owner);
        }
        else
        {
            RemoveOrganicSuit(ent.Owner, state);
            _audio.PlayPvs(MutationRetractSound, ent.Owner);
        }

        _popup.PopupEntity(
            Loc.GetString(state.OrganicSpaceSuit
                ? "changeling-organic-space-suit-formed"
                : "changeling-organic-space-suit-retracted"),
            ent.Owner,
            ent.Owner);
    }

    private void RemoveOrganicSuit(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        state.OrganicSpaceSuit = false;
        RemComp<ChangelingOrganicSpaceSuitComponent>(uid);
        UpdateEnvironmentalProtection(uid, state);
        RemoveEquippedVisual(uid, "outerClothing", state.OrganicSpaceSuitVisual);
        RemoveEquippedVisual(uid, "head", state.OrganicSpaceSuitHelmetVisual);
        state.OrganicSpaceSuitVisual = null;
        state.OrganicSpaceSuitHelmetVisual = null;
        RestoreStoredItem(uid, "outerClothing", StoredSuitOuterClothing);
        RestoreStoredItem(uid, "head", StoredSuitHead);
        _resources.RemoveChemicalRegenerationModifier(uid, OrganicSuitRegenKey);
    }

    private void UpdateEnvironmentalProtection(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        var required = state.OrganicSpaceSuit || state.VoidAdaptation;
        if (!required)
        {
            if (state.AddedPressureImmunity)
                RemComp<PressureImmunityComponent>(uid);
            if (state.AddedTemperatureProtection)
                RemComp<TemperatureProtectionComponent>(uid);

            RemComp<ChangelingEnvironmentalProtectionComponent>(uid);
            state.AddedPressureImmunity = false;
            state.AddedTemperatureProtection = false;
            return;
        }

        if (!HasComp<PressureImmunityComponent>(uid))
        {
            AddComp<PressureImmunityComponent>(uid);
            state.AddedPressureImmunity = true;
        }

        if (!HasComp<TemperatureProtectionComponent>(uid))
        {
            AddComp<TemperatureProtectionComponent>(uid);
            state.AddedTemperatureProtection = true;
        }

        var protection = EnsureComp<ChangelingEnvironmentalProtectionComponent>(uid);
        var coefficient = state.VoidAdaptation ? state.VoidTemperatureCoefficient : 1f;
        if (state.OrganicSpaceSuit)
            coefficient = Math.Min(coefficient, 0.05f);
        protection.TemperatureCoefficient = coefficient;
        protection.RespirationImmunity = state.VoidAdaptation;
    }

    private bool TryEquipOrganicSuitVisuals(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        if (!StoreEquippedItem(uid, "outerClothing", StoredSuitOuterClothing) ||
            !StoreEquippedItem(uid, "head", StoredSuitHead))
        {
            RestoreStoredItem(uid, "outerClothing", StoredSuitOuterClothing);
            RestoreStoredItem(uid, "head", StoredSuitHead);
            return false;
        }

        var coordinates = Transform(uid).Coordinates;
        var suit = Spawn(OrganicSpaceSuitVisual, coordinates);
        if (!_inventory.TryEquip(uid, suit, "outerClothing", silent: true, force: true))
        {
            QueueDel(suit);
            RestoreStoredItem(uid, "outerClothing", StoredSuitOuterClothing);
            RestoreStoredItem(uid, "head", StoredSuitHead);
            return false;
        }

        var helmet = Spawn(OrganicSpaceSuitHelmetVisual, coordinates);
        if (!_inventory.TryEquip(uid, helmet, "head", silent: true, force: true))
        {
            QueueDel(helmet);
            RemoveEquippedVisual(uid, "outerClothing", suit);
            RestoreStoredItem(uid, "outerClothing", StoredSuitOuterClothing);
            RestoreStoredItem(uid, "head", StoredSuitHead);
            return false;
        }

        state.OrganicSpaceSuitVisual = suit;
        state.OrganicSpaceSuitHelmetVisual = helmet;
        return true;
    }

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

    private void OnEnvironmentalTemperatureProtection(
        Entity<ChangelingEnvironmentalProtectionComponent> ent,
        ref GetTemperatureProtectionEvent args)
    {
        args.Coefficient *= ent.Comp.TemperatureCoefficient;
    }

    private void OnBeforeRespirationCycle(
        Entity<ChangelingEnvironmentalProtectionComponent> ent,
        ref BeforeRespirationCycleEvent args)
    {
        if (ent.Comp.RespirationImmunity)
            args.Cancelled = true;
    }
}
