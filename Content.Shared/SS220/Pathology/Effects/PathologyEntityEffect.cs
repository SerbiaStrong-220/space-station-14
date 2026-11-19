// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>
/// Applies regular entity effects (popups, emotes, vomit, stuns) to the pathology holder every update interval.
/// Use effect probability to make them occasional.
/// </summary>
public sealed partial class PathologyEntityEffect : IPathologyEffect
{
    [DataField(required: true)]
    public EntityEffect[] Effects = Array.Empty<EntityEffect>();

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        entityManager.System<SharedEntityEffectsSystem>().ApplyEffects(uid, Effects);
    }
}
