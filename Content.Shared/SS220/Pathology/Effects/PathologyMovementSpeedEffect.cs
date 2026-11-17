// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>
/// Continuously refreshes a movement slowdown status effect while the pathology is active.
/// </summary>
public sealed partial class PathologyMovementSpeedEffect : IPathologyEffect
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    [DataField]
    public float WalkSpeedModifier = 0.75f;

    [DataField]
    public float SprintSpeedModifier = 0.75f;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        var movementMod = entityManager.System<MovementModStatusSystem>();
        movementMod.TryUpdateMovementSpeedModDuration(uid, StatusEffect, null, WalkSpeedModifier, SprintSpeedModifier);
    }
}
