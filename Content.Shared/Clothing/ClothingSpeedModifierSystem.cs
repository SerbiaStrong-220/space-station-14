using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.SS220.PhysicalParameters;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class ClothingSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly PhysicalParametersSystem _parameters = default!;//SS220 add physical parameters

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<ClothingSpeedModifierComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnGetState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpeedModifierComponentState(component.WalkModifier, component.SprintModifier);
    }

    private void OnHandleState(EntityUid uid, ClothingSpeedModifierComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClothingSpeedModifierComponentState state)
            return;

        var diff = !MathHelper.CloseTo(component.SprintModifier, state.SprintModifier) ||
                   !MathHelper.CloseTo(component.WalkModifier, state.WalkModifier);

        component.WalkModifier = state.WalkModifier;
        component.SprintModifier = state.SprintModifier;

        // Avoid raising the event for the container if nothing changed.
        // We'll still set the values in case they're slightly different but within tolerance.
        if (diff && _container.TryGetContainingContainer((uid, null, null), out var container))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, ClothingSpeedModifierComponent component, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (component.RequireActivated && !_toggle.IsActivated(uid))
            return;

        if (component.Standing != null && !_standing.IsMatchingState(args.Owner, component.Standing.Value))
            return;

        //SS220 add physical parameters begin
        if (component.AffectedByParameters &&
            TryComp<ItemExtensionComponent>(uid, out var itemExtensionComp) &&
            TryComp<PhysicalParametersComponent>(args.Owner, out var parametersComp))
        {
            var ownerParameter = _parameters.GetParameterValue((args.Owner, parametersComp), Parameter.Strength, armStrengthCounted: false);

            float parameterMultiplier = 0f;

            if (itemExtensionComp.StrengthRequirementToBeUsed != itemExtensionComp.MinimalStrengthToPickUp)
                parameterMultiplier = FixedPoint2.Clamp(1 - (ownerParameter - itemExtensionComp.MinimalStrengthToPickUp) / (itemExtensionComp.StrengthRequirementToBeUsed - itemExtensionComp.MinimalStrengthToPickUp), FixedPoint2.Zero, 1).Float();

            args.Args.ModifySpeed(1 - (1 - component.WalkModifier) * parameterMultiplier, 1 - (1 - component.SprintModifier) * parameterMultiplier);
            return;
        }
        //SS220 add physical parameters end

        args.Args.ModifySpeed(component.WalkModifier, component.SprintModifier);
    }
    private void OnToggled(Entity<ClothingSpeedModifierComponent> ent, ref ItemToggledEvent args)
    {
        if (!ent.Comp.RequireActivated)
            return;

        // make sentient boots slow or fast too
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
        {
            // inventory system will automatically hook into the event raised by this and update accordingly
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }
}
