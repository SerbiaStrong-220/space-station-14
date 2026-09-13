// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport.Components;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class AfterTeleportStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffect.StatusEffectsSystem _statusEffectsOld = default!;
    [Dependency] private StatusEffectNew.StatusEffectsSystem _statusEffectsNew = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterTeleportStatusEffectComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<AfterTeleportStatusEffectComponent> ent, ref TargetTeleportedEvent args)
    {
        foreach (var (effect, duration) in ent.Comp.EffectsList)
        {
            _statusEffectsNew.TryAddStatusEffectDuration(args.Target, effect, duration);
            _statusEffectsOld.TryAddStatusEffect(args.Target, effect, duration, false);//because some effects are in the old form
        }
    }
}
