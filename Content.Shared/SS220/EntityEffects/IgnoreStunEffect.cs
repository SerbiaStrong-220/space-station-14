using Content.Shared.EntityEffects;
using Content.Shared.SS220.IgnoreStunEffect;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects;

public sealed partial class IgnoreStunEffect : EntityEffect
{
    [DataField]
    public float Duration;

    [DataField(required: true)]
    public HashSet<string> RequiredEffects;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return string.Empty;
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var ignoreStun = args.EntityManager.EnsureComponent<IgnoreStunEffectComponent>(args.TargetEntity);
        ignoreStun.RequiredEffects = RequiredEffects;
        ignoreStun.Time = Duration;

        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out StatusEffectsComponent? effectsComponent))
            return;

        ignoreStun.OriginalEffects = [..effectsComponent.AllowedEffects];

        effectsComponent.AllowedEffects.RemoveAll(e => ignoreStun.RequiredEffects.Contains(e));
    }
}
