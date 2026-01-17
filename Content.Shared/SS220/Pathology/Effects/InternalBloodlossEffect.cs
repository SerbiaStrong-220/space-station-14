// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class InternalBloodLossEffect : IPathologyEffect
{
    /// <summary>
    /// Blood loss per update interval and per stack
    /// </summary>
    [DataField]
    public FixedPoint2 LossRate = 0.25f;

    private readonly ProtoId<EmotePrototype> _bloodCoughEmote = "BloodCough";

    /// <summary>
    /// Somewhat good value to make minor IBs less noticeable and major one being annoyed in proper way (in response to default 300 volume)
    /// </summary>
    private readonly float _baseCoughChancePerStack = 0.01f;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        var bloodSystem = entityManager.System<SharedBloodstreamSystem>();

        var bloodLoss = LossRate * data.StackCount * SharedPathologySystem.UpdateInterval.TotalSeconds;

        bloodSystem.TryModifyBloodLevel(uid, -bloodLoss);
        var netEntity = entityManager.GetNetEntity(uid);

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)entityManager.CurrentTick.Value, bloodLoss.Int(), netEntity.Id });
        var rand = new System.Random(seed);

        if (!rand.Prob(_baseCoughChancePerStack * data.StackCount))
            return;

        entityManager.System<SharedChatSystem>().TryEmoteWithChat(uid, _bloodCoughEmote);
    }
}
