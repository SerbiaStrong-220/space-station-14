// SS220 Changeling
using Content.Server.Changeling.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Doors.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.Stealth.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    private void InitializeAdaptations()
    {
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingAugmentedEyesightActionEvent>(OnAugmentedEyesight);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingChameleonSkinActionEvent>(OnChameleonSkin);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingContortBodyActionEvent>(OnContortBody);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingDigitalCamouflageActionEvent>(OnDigitalCamouflage);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingDarknessAdaptationActionEvent>(OnDarknessAdaptation);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingVoidAdaptationActionEvent>(OnVoidAdaptation);
        SubscribeLocalEvent<ChangelingContortedComponent, PreventCollideEvent>(OnContortedPreventCollide);
    }

    private void UpdateAdaptations(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ChangelingUtilityStateComponent>();
        while (query.MoveNext(out var uid, out var state))
        {
            if (state.DarknessAdaptation && now >= state.NextDarknessUpdate)
            {
                state.NextDarknessUpdate += state.DarknessUpdateInterval;
                UpdateDarknessConcealment(uid, state);
            }

            if (state.VoidAdaptation && now >= state.NextVoidUpkeep)
            {
                state.NextVoidUpkeep += state.VoidUpkeepInterval;
                if (!IsValidChemicalAmount(state.VoidUpkeepCost) ||
                    !_resources.TrySpendChemicals(uid, FixedPoint2.New(state.VoidUpkeepCost)))
                {
                    DisableVoidAdaptation(uid, state, showPopup: true, updateAction: true);
                }
            }
        }
    }

    private void OnAugmentedEyesight(Entity<ChangelingResourceComponent> ent, ref ChangelingAugmentedEyesightActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;
        args.Handled = true;
        args.Toggle = true;

        var state = EnsureState(ent);
        state.AugmentedEyesight = !state.AugmentedEyesight;
        if (state.AugmentedEyesight)
        {
            if (!HasComp<EyeProtectionComponent>(ent))
            {
                AddComp<EyeProtectionComponent>(ent);
                state.AddedEyeProtection = true;
            }

            if (!HasComp<ThermalVisionComponent>(ent))
            {
                AddComp(ent.Owner, new ThermalVisionComponent(14f, 5f) { State = IgnoreLightVisionOverlayState.Full });
                state.AddedThermalVision = true;
            }
        }
        else
        {
            if (state.AddedEyeProtection)
                RemComp<EyeProtectionComponent>(ent);
            if (state.AddedThermalVision)
                RemComp<ThermalVisionComponent>(ent);
            state.AddedEyeProtection = false;
            state.AddedThermalVision = false;
        }

        _popup.PopupEntity(
            Loc.GetString(state.AugmentedEyesight
                ? "changeling-augmented-eyesight-enabled"
                : "changeling-augmented-eyesight-disabled"),
            ent.Owner,
            ent.Owner);
    }

    private void OnChameleonSkin(Entity<ChangelingResourceComponent> ent, ref ChangelingChameleonSkinActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureState(ent);
        if (!state.ChameleonSkin && !Spend(ent.Owner, 25))
            return;

        state.ChameleonSkin = !state.ChameleonSkin;
        args.Handled = true;
        args.Toggle = true;
        UpdateStealth(ent.Owner, state);
        _popup.PopupEntity(
            Loc.GetString(state.ChameleonSkin
                ? "changeling-chameleon-skin-enabled"
                : "changeling-chameleon-skin-disabled"),
            ent.Owner,
            ent.Owner);
    }

    private void OnDarknessAdaptation(Entity<ChangelingResourceComponent> ent, ref ChangelingDarknessAdaptationActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureState(ent);
        if (!state.DarknessAdaptation &&
            (!IsValidChemicalAmount(args.ChemicalCost) ||
             !float.IsFinite(args.RegenerationModifier) ||
             !float.IsFinite(args.DarknessVisibility) ||
             !float.IsFinite(args.LightSearchRadius) ||
             !float.IsFinite(args.LightThreshold)))
        {
            return;
        }

        if (!state.DarknessAdaptation && !Spend(ent.Owner, Math.Max(0f, args.ChemicalCost)))
            return;

        state.DarknessAdaptation = !state.DarknessAdaptation;
        args.Handled = true;
        args.Toggle = true;
        if (state.DarknessAdaptation)
        {
            state.DarknessUpdateInterval = TimeSpan.FromTicks(Math.Clamp(
                args.UpdateInterval.Ticks,
                TimeSpan.FromMilliseconds(100).Ticks,
                MaxAdaptationInterval.Ticks));
            state.DarknessVisibility = Math.Clamp(args.DarknessVisibility, -1f, 1f);
            state.DarknessLightSearchRadius = Math.Clamp(args.LightSearchRadius, 1f, 32f);
            state.DarknessLightThreshold = Math.Max(0.01f, args.LightThreshold);
            state.NextDarknessUpdate = _timing.CurTime;
            _resources.SetChemicalRegenerationModifier(ent.Owner,
                DarknessRegenKey,
                Math.Clamp(args.RegenerationModifier, 0f, 1f));
            UpdateDarknessConcealment(ent.Owner, state);
        }
        else
        {
            state.DarknessConcealmentActive = false;
            _resources.RemoveChemicalRegenerationModifier(ent.Owner, DarknessRegenKey);
            UpdateStealth(ent.Owner, state);
        }

        _popup.PopupEntity(
            Loc.GetString(state.DarknessAdaptation
                ? "changeling-darkness-adaptation-enabled"
                : "changeling-darkness-adaptation-disabled"),
            ent.Owner,
            ent.Owner);
    }

    private void UpdateDarknessConcealment(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        var concealed = IsInDarkness(uid, state);
        if (state.DarknessConcealmentActive == concealed)
            return;

        state.DarknessConcealmentActive = concealed;
        UpdateStealth(uid, state);
        _popup.PopupEntity(
            Loc.GetString(concealed
                ? "changeling-darkness-adaptation-concealed"
                : "changeling-darkness-adaptation-revealed"),
            uid,
            uid);
    }

    private bool IsInDarkness(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        var coordinates = _transform.GetMapCoordinates(uid);
        var xform = Transform(uid);
        if (xform.MapUid is { } mapUid && TryComp<MapLightComponent>(mapUid, out var mapLight))
        {
            var ambient = Math.Max(mapLight.AmbientLightColor.R,
                Math.Max(mapLight.AmbientLightColor.G, mapLight.AmbientLightColor.B));
            if (ambient >= state.DarknessLightThreshold)
                return false;
        }

        _nearbyLights.Clear();
        _lookup.GetEntitiesInRange(coordinates, state.DarknessLightSearchRadius, _nearbyLights);
        foreach (var light in _nearbyLights)
        {
            if (!light.Comp.Enabled ||
                light.Comp.ContainerOccluded ||
                light.Comp.Energy <= 0f ||
                light.Comp.Radius <= 0f)
            {
                continue;
            }

            var lightCoordinates = _transform.GetMapCoordinates(light.Owner);
            var distance = (coordinates.Position - lightCoordinates.Position).Length();
            if (distance > light.Comp.Radius ||
                !_interaction.InRangeUnobstructed(lightCoordinates, coordinates, light.Comp.Radius))
            {
                continue;
            }

            var colorStrength = Math.Max(light.Comp.Color.R, Math.Max(light.Comp.Color.G, light.Comp.Color.B));
            var brightness = light.Comp.Energy * colorStrength * (1f - distance / light.Comp.Radius);
            if (brightness >= state.DarknessLightThreshold)
                return false;
        }

        return true;
    }

    private void OnVoidAdaptation(Entity<ChangelingResourceComponent> ent, ref ChangelingVoidAdaptationActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureState(ent);
        if (!state.VoidAdaptation &&
            (!IsValidChemicalAmount(args.ChemicalCost) ||
             !IsValidChemicalAmount(args.UpkeepCost) ||
             !float.IsFinite(args.TemperatureCoefficient)))
        {
            return;
        }

        if (!state.VoidAdaptation && !Spend(ent.Owner, Math.Max(0f, args.ChemicalCost)))
            return;

        state.VoidAdaptation = !state.VoidAdaptation;
        args.Handled = true;
        args.Toggle = true;
        if (state.VoidAdaptation)
        {
            state.VoidUpkeepCost = Math.Max(0f, args.UpkeepCost);
            state.VoidUpkeepInterval = TimeSpan.FromTicks(Math.Clamp(
                args.UpkeepInterval.Ticks,
                TimeSpan.FromMilliseconds(250).Ticks,
                MaxAdaptationInterval.Ticks));
            state.VoidTemperatureCoefficient = Math.Clamp(args.TemperatureCoefficient, 0f, 1f);
            state.NextVoidUpkeep = _timing.CurTime + state.VoidUpkeepInterval;
            UpdateEnvironmentalProtection(ent.Owner, state);
            _popup.PopupEntity(Loc.GetString("changeling-void-adaptation-enabled"), ent.Owner, ent.Owner);
            return;
        }

        DisableVoidAdaptation(ent.Owner, state, showPopup: true, updateAction: false);
    }

    private void DisableVoidAdaptation(
        EntityUid uid,
        ChangelingUtilityStateComponent state,
        bool showPopup,
        bool updateAction)
    {
        state.VoidAdaptation = false;
        UpdateEnvironmentalProtection(uid, state);
        if (updateAction)
        {
            foreach (var action in _actions.GetActions(uid))
            {
                if (MetaData(action).EntityPrototype?.ID != VoidAdaptationAction.Id)
                    continue;

                _actions.SetToggled(action.AsNullable(), false);
                break;
            }
        }

        if (showPopup)
            _popup.PopupEntity(Loc.GetString("changeling-void-adaptation-disabled"), uid, uid);
    }

    private void UpdateStealth(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        if (!state.ChameleonSkin && !state.DarknessConcealmentActive)
        {
            if (state.AddedStealth)
                RemComp<StealthComponent>(uid);

            if (!state.AddedStealth &&
                state.OriginalStealthCaptured &&
                TryComp<StealthComponent>(uid, out var originalStealth))
            {
                _stealth.SetVisibility(uid, state.OriginalStealthVisibility, originalStealth);
                _stealth.SetEnabled(uid, state.OriginalStealthEnabled, originalStealth);
            }

            state.AddedStealth = false;
            state.OriginalStealthCaptured = false;
            return;
        }

        if (!TryComp<StealthComponent>(uid, out var stealth))
        {
            stealth = AddComp<StealthComponent>(uid);
            state.AddedStealth = true;
        }
        else if (!state.AddedStealth && !state.OriginalStealthCaptured)
        {
            state.OriginalStealthCaptured = true;
            state.OriginalStealthEnabled = stealth.Enabled;
            state.OriginalStealthVisibility = _stealth.GetVisibility(uid, stealth);
        }

        _stealth.SetEnabled(uid, true, stealth);
        _stealth.SetVisibility(uid, state.ChameleonSkin ? -1f : state.DarknessVisibility, stealth);
    }

    private void OnContortBody(Entity<ChangelingResourceComponent> ent, ref ChangelingContortBodyActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        var state = EnsureState(ent);
        if (!state.Contorted && !Spend(ent.Owner, 25))
            return;

        state.Contorted = !state.Contorted;
        args.Handled = true;
        args.Toggle = true;
        if (state.Contorted)
            EnsureComp<ChangelingContortedComponent>(ent);
        else
            RemComp<ChangelingContortedComponent>(ent);

        _popup.PopupEntity(
            Loc.GetString(state.Contorted
                ? "changeling-contort-body-enabled"
                : "changeling-contort-body-disabled"),
            ent.Owner,
            ent.Owner);
    }

    private void OnDigitalCamouflage(Entity<ChangelingResourceComponent> ent, ref ChangelingDigitalCamouflageActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;
        args.Toggle = true;
        var state = EnsureState(ent);
        state.DigitalCamouflage = !state.DigitalCamouflage;
        if (state.DigitalCamouflage)
            EnsureComp<ChangelingDigitalCamouflageComponent>(ent);
        else
            RemComp<ChangelingDigitalCamouflageComponent>(ent);

        _popup.PopupEntity(
            Loc.GetString(state.DigitalCamouflage
                ? "changeling-digital-camouflage-enabled"
                : "changeling-digital-camouflage-disabled"),
            ent.Owner,
            ent.Owner);
    }

    private void OnContortedPreventCollide(Entity<ChangelingContortedComponent> ent, ref PreventCollideEvent args)
    {
        if (!TryComp<DoorComponent>(args.OtherEntity, out var door) || door.State == DoorState.Welded)
            return;

        if (TryComp<DoorBoltComponent>(args.OtherEntity, out var bolts) && bolts.BoltsDown)
            return;

        // A contorted changeling can slide beneath an unbolted door without opening it.
        args.Cancelled = true;
    }
}
