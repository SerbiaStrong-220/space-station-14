// SS220 Changeling
using Content.Server.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Gibbing;
using Content.Shared.SS220.IgnoreLightVision.Components;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    private void InitializeCleanup()
    {
        SubscribeLocalEvent<ChangelingUtilityStateComponent, ChangelingResourceRemovedEvent>(OnResourceRemoved);
        SubscribeLocalEvent<ChangelingUtilityStateComponent, ChangelingEvolutionResetEvent>(OnEvolutionReset);
        SubscribeLocalEvent<ChangelingUtilityStateComponent, BeingGibbedEvent>(OnUtilityStateGibbed);
        SubscribeLocalEvent<ChangelingUtilityStateComponent, ComponentShutdown>(OnUtilityStateShutdown);
    }

    private void OnResourceRemoved(
        Entity<ChangelingUtilityStateComponent> ent,
        ref ChangelingResourceRemovedEvent args)
    {
        CleanupUtilityStateForRemoval(ent, dropStoredItems: args.EntityTerminating);
        RemCompDeferred<ChangelingUtilityStateComponent>(ent);
    }

    private void OnEvolutionReset(
        Entity<ChangelingUtilityStateComponent> ent,
        ref ChangelingEvolutionResetEvent args)
    {
        CleanupUtilityStateForRemoval(ent, dropStoredItems: false);
        RemCompDeferred<ChangelingUtilityStateComponent>(ent);
    }

    private void OnUtilityStateGibbed(Entity<ChangelingUtilityStateComponent> ent, ref BeingGibbedEvent args)
    {
        CleanupUtilityStateForRemoval(ent, dropStoredItems: true);
    }

    private void OnUtilityStateShutdown(Entity<ChangelingUtilityStateComponent> ent, ref ComponentShutdown args)
    {
        CleanupUtilityStateForRemoval(ent, TerminatingOrDeleted(ent.Owner));
    }

    private void CleanupUtilityStateForRemoval(Entity<ChangelingUtilityStateComponent> ent, bool dropStoredItems)
    {
        var state = ent.Comp;
        state.AugmentedEyesight = false;
        if (state.AddedEyeProtection)
            RemComp<EyeProtectionComponent>(ent);
        if (state.AddedThermalVision)
            RemComp<ThermalVisionComponent>(ent);
        state.AddedEyeProtection = false;
        state.AddedThermalVision = false;

        state.ChameleonSkin = false;
        state.DarknessAdaptation = false;
        state.DarknessConcealmentActive = false;
        UpdateStealth(ent.Owner, state);
        _resources.RemoveChemicalRegenerationModifier(ent.Owner, DarknessRegenKey);

        RestoreTelepathy(ent.Owner, state);
        state.DigitalCamouflage = false;
        state.Contorted = false;
        RemComp<ChangelingDigitalCamouflageComponent>(ent);
        RemComp<ChangelingContortedComponent>(ent);

        state.VoidAdaptation = false;
        state.OrganicSpaceSuit = false;
        RemComp<ChangelingOrganicSpaceSuitComponent>(ent);
        UpdateEnvironmentalProtection(ent.Owner, state);
        RemoveEquippedVisual(ent.Owner, "outerClothing", state.OrganicSpaceSuitVisual);
        RemoveEquippedVisual(ent.Owner, "head", state.OrganicSpaceSuitHelmetVisual);
        state.OrganicSpaceSuitVisual = null;
        state.OrganicSpaceSuitHelmetVisual = null;
        if (dropStoredItems)
        {
            DropStoredItem(ent.Owner, StoredSuitOuterClothing);
            DropStoredItem(ent.Owner, StoredSuitHead);
        }
        else
        {
            RestoreStoredItem(ent.Owner, "outerClothing", StoredSuitOuterClothing);
            RestoreStoredItem(ent.Owner, "head", StoredSuitHead);
        }

        _resources.RemoveChemicalRegenerationModifier(ent.Owner, OrganicSuitRegenKey);

        RestoreOriginalVoice(ent.Owner, state);
        ClearPendingTransformation(ent.Owner, state);
        UntogglePurchasedActions(ent.Owner);
    }
}
