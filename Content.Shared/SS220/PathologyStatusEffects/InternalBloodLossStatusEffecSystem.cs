// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.PathologyStatusEffects;

/// <summary>
/// Applies the drunk status effect to this entity.
/// The duration of the effect is equal to <see cref="Drunk.BoozePower"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class InternalBloodLossStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    private const float UpdateRate = 0.8f;

    /// <summary>
    /// it actually gives (1 + CaughtPart) multiplier to bloodloss, so be careful
    /// </summary>
    private const float CaughtPart = 0.2f;

    private static readonly TimeSpan BleedingTimeUpdate = TimeSpan.FromSeconds(UpdateRate);
    private static readonly FixedPoint2 DecreaseLoss = 0.6f;

    private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;
    private HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalBloodLossStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<InternalBloodLossStatusEffectComponent, StatusEffectRelayedEvent<ApplyMetabolicMultiplierEvent>>(OnMetabolicMultiplierApply);

        _bloodstreamQuery = GetEntityQuery<BloodstreamComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _toRemove.Clear();

        var query = EntityQueryEnumerator<InternalBloodLossStatusEffectComponent, PathologyStatusEffectStackableComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var internalBloodLoss, out var stackableComponent, out var statusEffect))
        {
            if (_gameTiming.CurTime < internalBloodLoss.NextEffectTime)
                continue;

            internalBloodLoss.NextEffectTime = _gameTiming.CurTime + BleedingTimeUpdate;

            if (!_bloodstreamQuery.TryComp(statusEffect.AppliedTo, out var bloodstreamComponent))
            {
                _toRemove.Add(uid);
                continue;
            }

            var bloodlossCorrectCount = GetCorrectStack(stackableComponent.StackCount);
            var bloodLoss = internalBloodLoss.BloodLossRatePerStack * internalBloodLoss.Multiplier * bloodlossCorrectCount * UpdateRate;

            // Add some 'last stand' effect for ux
            if (bloodstreamComponent.BloodSolution is { } bloodSolution)
            {
                var lostBloodPortion = bloodSolution.Comp.Solution.Volume / bloodSolution.Comp.Solution.MaxVolume;
                bloodLoss = lostBloodPortion < DecreaseLoss ? bloodLoss * lostBloodPortion : bloodLoss;
            }

            ProcessInternalBloodLoss((statusEffect.AppliedTo.Value, bloodstreamComponent), bloodLoss);
        }

        foreach (var uid in _toRemove)
        {
            // Okay this is straight way, no idea will it work in future, but kinda have to
            PredictedQueueDel(uid);
        }
    }

    private void ProcessInternalBloodLoss(Entity<BloodstreamComponent> entity, FixedPoint2 bloodLoss)
    {
        if (!_bloodstream.TryModifyBloodLevel(entity!, -bloodLoss))
            return;

        entity.Comp.InternalBleedingBloodAccumulator += bloodLoss;
        DirtyField(entity!, nameof(BloodstreamComponent.InternalBleedingBloodAccumulator));

        if (entity.Comp.InternalBleedingBloodAccumulator < entity.Comp.BloodAmountToCough)
            return;

        // this is actually gives (1 + CaughtPart) multiplier to bloodloss, so be careful
        _bloodstream.TryBleedOut(entity!, entity.Comp.InternalBleedingBloodAccumulator * CaughtPart);

        entity.Comp.InternalBleedingBloodAccumulator = FixedPoint2.Zero;
        _chat.TryEmoteWithChat(entity, entity.Comp.BloodCoughEmote);
    }

    private void OnStatusEffectRemoved(Entity<InternalBloodLossStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
            return;

        bloodstream.InternalBleedingBloodAccumulator = FixedPoint2.Zero;
        DirtyField(args.Target, bloodstream, nameof(BloodstreamComponent.InternalBleedingBloodAccumulator));
    }

    private void OnMetabolicMultiplierApply(Entity<InternalBloodLossStatusEffectComponent> entity, ref StatusEffectRelayedEvent<ApplyMetabolicMultiplierEvent> args)
    {
        entity.Comp.Multiplier = args.Args.Multiplier;
    }

    private const float CoeffA = 3.2f;
    private const float CoeffB = 0.1f;
    private const float CoeffC = 4f;
    /// <summary>
    /// Implements formula for better ux:
    /// result stack is 1 + a(x-1) / (b(x-1) + c)
    /// this allows to fine tune how much bullet stacks cause bleeds while stacking.
    /// Use any graphic tool to see the actual y(x)
    /// </summary>
    private static float GetCorrectStack(float stackCount)
    {
        return 1f + CoeffA * (stackCount - 1) / (CoeffB * (stackCount - 1) + CoeffC);
    }
}
