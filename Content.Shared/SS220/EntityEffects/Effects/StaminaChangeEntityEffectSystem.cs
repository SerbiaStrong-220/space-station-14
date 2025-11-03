// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class StaminaDamageEntityEffectSystem : EntityEffectSystem<StaminaComponent, StaminaChange>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<StaminaChange> args)
    {
        // TODO: Replace with proper random prediciton when it exists.
        if (args.Effect.Probability <= 1f)
        {
            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(entity).Id, 0 });
            var rand = new System.Random(seed);
            if (!rand.Prob(args.Effect.Probability))
                return;
        }

        _stamina.TakeStaminaDamage(entity, args.Effect.Value, visual: false, ignoreResist: true);
    }
}
