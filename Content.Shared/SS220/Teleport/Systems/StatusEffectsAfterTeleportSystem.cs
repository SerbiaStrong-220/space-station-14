// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport.Components;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class StatusEffectsAfterTeleportSystem : EntitySystem
{
    [Dependency] private StatusEffect.StatusEffectsSystem _legacyStatusEffects = default!;
    [Dependency] private StatusEffectNew.StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectsAfterTeleportComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<StatusEffectsAfterTeleportComponent> ent, ref TargetTeleportedEvent args)
    {
        foreach (var (effect, duration) in ent.Comp.Effects)
        {
            _statusEffects.TryAddStatusEffectDuration(args.Target, effect, duration);
            // Some effects still use the legacy status-effect implementation.
            _legacyStatusEffects.TryAddStatusEffect(args.Target, effect, duration, false);
        }
    }
}
