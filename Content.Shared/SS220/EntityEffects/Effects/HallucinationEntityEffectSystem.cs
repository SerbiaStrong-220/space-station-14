// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.SS220.Hallucination;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class HallucinationEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Hallucination>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Hallucination> args)
    {
        var hallucinationSystem = EntityManager.System<SharedHallucinationSystem>();
        hallucinationSystem.TryAdd(entity, args.Effect.Setting);
    }
}
