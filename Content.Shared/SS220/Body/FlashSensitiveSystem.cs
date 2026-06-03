using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash;
using Content.Shared.SS220.Flash;
using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.Body;

public sealed partial class FlashSensitiveSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashSensitiveComponent, BodyRelayedEvent<BeforeFlashedEvent>>(OnBeforeFlashed);
        SubscribeLocalEvent<FlashSensitiveComponent, BodyRelayedEvent<AfterFlashedEvent>>(OnAfterFlashed);
    }

    private void OnBeforeFlashed(Entity<FlashSensitiveComponent> ent, ref BodyRelayedEvent<BeforeFlashedEvent> args)
    {
        if (args.Args.Target != args.Body.Owner)
            return;

        var temp = args.Args;
        temp.FlashDurationMultiplier *= ent.Comp.FlashDurationMultiplier;
        temp.StunDurationMultiplier *= ent.Comp.StunDurationMultiplier;
        args.Args = temp;
    }

    private void OnAfterFlashed(Entity<FlashSensitiveComponent> ent, ref BodyRelayedEvent<AfterFlashedEvent> args)
    {
        if (args.Args.Target != args.Body.Owner)
            return;

        if (ent.Comp.FlashEyeDamage is { } eyeDamage)
            _blindable.AdjustEyeDamage(args.Body.Owner, eyeDamage);

        if (ent.Comp.TemporaryBlindnessDuration is { } tempBlindDuraion)
            // SS220 ToDo: change obsolete method after refactoring TemporaryBlindness status effect 
            _statusEffects.TryAddStatusEffect(args.Body, TemporaryBlindnessSystem.BlindingStatusEffect,
                tempBlindDuraion, false, TemporaryBlindnessSystem.BlindingStatusEffect);
    }
}
