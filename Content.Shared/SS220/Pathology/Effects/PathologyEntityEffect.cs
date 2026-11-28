// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>
/// Applies regular entity effects (damage, popups, emotes, vomit, stuns) to the pathology holder every update interval.
/// Use effect probability to make them occasional.
/// </summary>
public sealed partial class PathologyEntityEffect : IPathologyEffect
{
    [DataField(required: true)]
    public EntityEffect[] Effects = Array.Empty<EntityEffect>();

    /// <summary>
    /// If set, the effects are applied with this chance for each stack, so they happen more often the more stacks there are.
    /// </summary>
    [DataField]
    public float? ChancePerStack;

    /// <summary>
    /// Whether the effects are scaled by the stack count, e.g. damage per stack.
    /// </summary>
    [DataField]
    public bool ScaleByStacks;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        if (ChancePerStack is { } chance && !entityManager.System<SharedPathologySystem>().RollChancePerStack(data, chance))
            return;

        entityManager.System<SharedEntityEffectsSystem>().ApplyEffects(uid, Effects, ScaleByStacks ? data.StackCount : 1f);
    }
}
