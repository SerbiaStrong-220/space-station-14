// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class InternalBloodLossEffect : IPathologyEffect
{
    /// <summary>
    /// Blood loss per update interval and per stack
    /// </summary>
    [DataField]
    public FixedPoint2 LossRate = 0.3f;

    private readonly ProtoId<EmotePrototype> _bloodCoughEmote = "BloodCough";

    /// <summary>
    /// Somewhat good value to make minor IBs less noticeable and major one being annoyed in proper way (in response to default 300 volume)
    /// </summary>
    private readonly FixedPoint2 _bloodLossPerCough = 10f;

    private FixedPoint2 _bloodLossAccumulator = 0f;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        var bloodSystem = entityManager.System<SharedBloodstreamSystem>();

        var bloodLoss = LossRate * data.StackCount * PathologySystem.UpdateInterval.TotalSeconds;

        bloodSystem.TryModifyBloodLevel(uid, bloodLoss);

        _bloodLossAccumulator += bloodLoss;

        if (_bloodLossAccumulator < _bloodLossPerCough)
            return;

        _bloodLossAccumulator -= _bloodLossPerCough;
        entityManager.System<SharedChatSystem>().TryEmoteWithChat(uid, _bloodCoughEmote);
    }
}
