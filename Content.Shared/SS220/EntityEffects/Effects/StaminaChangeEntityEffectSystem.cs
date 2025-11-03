// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class StaminaDamageEntityEffectSystem : EntityEffectSystem<StaminaComponent, StaminaChange>
{
    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<StaminaChange> args)
    {
        var stunSys = EntityManager.System<SharedStaminaSystem>();
        stunSys.TakeStaminaDamage(entity, args.Effect.Value, visual: false, ignoreResist: true);
    }
}
