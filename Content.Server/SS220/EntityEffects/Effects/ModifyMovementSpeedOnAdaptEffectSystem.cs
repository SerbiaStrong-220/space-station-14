
using Content.Server.SS220.LimitationRevive;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.ChemicalAdaptation;
using Content.Shared.SS220.EntityEffects.Effects;

namespace Content.Server.SS220.EntityEffects.Effects;

/// <summary>
/// Narrowly targeted effect to increase time to brain damage.
/// Uses ChemicalAdaptation to reduce the effectiveness of use
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ModifyMovementSpeedOnAdaptEffectSystem : EntityEffectSystem<LimitationReviveComponent, ModifyMovementSpeedOnAdapt>
{
    [Dependency] private readonly LimitationReviveSystem _limitationRevive = default!;
    [Dependency] private readonly ChemicalAdaptationSystem _chemicalAdaptation = default!;
    [Dependency] private readonly MovementModStatusSystem _movementModStatus = default!;

    protected override void Effect(Entity<LimitationReviveComponent> entity, ref EntityEffectEvent<ModifyMovementSpeedOnAdapt> args)
    {
    }
}
