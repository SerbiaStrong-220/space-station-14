// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class StaminaDamageEntityEffectSystem : EntityEffectSystem<StaminaComponent, StaminaChange>
{
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<StaminaChange> args)
    {
        if (!_random.Prob(args.Effect.Probability))
            return;

        _stamina.TakeStaminaDamage(entity, args.Effect.Value, visual: false, ignoreResist: true);
    }
}
