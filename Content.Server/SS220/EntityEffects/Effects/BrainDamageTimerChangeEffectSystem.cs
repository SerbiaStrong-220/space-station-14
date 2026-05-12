
using Content.Server.SS220.LimitationRevive;
using Content.Shared.EntityEffects;
using Content.Shared.SS220.ChemicalAdaptation;
using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.SS220.LimitationRevive;

namespace Content.Server.SS220.EntityEffects.Effects;

/// <summary>
/// Narrowly targeted effect to increase time to brain damage.
/// Uses ChemicalAdaptation to reduce the effectiveness of use
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class BrainDamageTimerChangeEffectSystem : EntityEffectSystem<LimitationReviveComponent, BrainDamageTimerChange>
{
    [Dependency] private readonly LimitationReviveSystem _limitationRevive = default!;
    [Dependency] private readonly ChemicalAdaptationSystem _chemicalAdaptation = default!;

    protected override void Effect(Entity<LimitationReviveComponent> entity, ref EntityEffectEvent<BrainDamageTimerChange> args)
    {
        var timeBuffer = args.Effect.AddTime;

        // SS220-todo-put it somewhere in parent
        if (_chemicalAdaptation.TryGetMetabolized(entity, args.Effect.Reagent, out var metabolized))
            timeBuffer *= Math.Pow(args.Effect.Decay, metabolized);

        _limitationRevive.IncreaseTimer(entity, timeBuffer);
    }
}
