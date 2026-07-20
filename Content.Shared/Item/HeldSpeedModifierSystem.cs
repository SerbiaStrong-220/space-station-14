using Content.Shared.Clothing;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.SS220.PhysicalParameters;

namespace Content.Shared.Item;

/// <summary>
/// This handles <see cref="HeldSpeedModifierComponent"/>
/// </summary>
public sealed class HeldSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PhysicalParametersSystem _parameters = default!;//SS220 add physical parameters

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HeldSpeedModifierComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<HeldSpeedModifierComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<HeldSpeedModifierComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
    }

    private void OnGotEquippedHand(Entity<HeldSpeedModifierComponent> ent, ref GotEquippedHandEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGotUnequippedHand(Entity<HeldSpeedModifierComponent> ent, ref GotUnequippedHandEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    public (float,float) GetHeldMovementSpeedModifiers(EntityUid uid, HeldSpeedModifierComponent component, EntityUid? owner = null) //SS220 add physical parameters
    {
        var walkMod = component.WalkModifier;
        var sprintMod = component.SprintModifier;

        ClothingSpeedModifierComponent? clothingSpeedModComp = null; //SS220 add physical parameters

        if (component.MirrorClothingModifier && TryComp<ClothingSpeedModifierComponent>(uid, out var clothingSpeedModifier))
        {
            walkMod = clothingSpeedModifier.WalkModifier;
            sprintMod = clothingSpeedModifier.SprintModifier;
            clothingSpeedModComp = clothingSpeedModifier; //SS220 add physical parameters
        }

        //SS220 add physical parameters begin
        if ((component.AffectedByParameters || (clothingSpeedModComp != null && clothingSpeedModComp.AffectedByParameters)) &&
            TryComp<ItemExtensionComponent>(uid, out var itemExtensionComp) &&
            owner is { Valid: true } ownerValidated &&
            TryComp<PhysicalParametersComponent>(ownerValidated, out var parametersComp))
        {
            float parameterMultiplier = 1f;

            var ownerParameter = _parameters.GetParameterValue((ownerValidated, parametersComp), Parameter.Strength, armStrengthCounted: false);

            if (itemExtensionComp.StrengthRequirementToBeUsed != itemExtensionComp.MinimalStrengthToPickUp)
                parameterMultiplier = FixedPoint2.Clamp(1 - (ownerParameter - itemExtensionComp.MinimalStrengthToPickUp) / (itemExtensionComp.StrengthRequirementToBeUsed - itemExtensionComp.MinimalStrengthToPickUp), FixedPoint2.Zero, 1).Float();

            walkMod = 1 - (1 - walkMod) * parameterMultiplier;
            sprintMod = 1 - (1 - sprintMod) * parameterMultiplier;
        }
        //SS220 add physical parameters end

        return (walkMod, sprintMod);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, HeldSpeedModifierComponent component, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        var (walkMod, sprintMod) = GetHeldMovementSpeedModifiers(uid, component, args.Owner); //SS220 add physical parameters
        args.Args.ModifySpeed(walkMod, sprintMod);
    }
}
