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
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private const float UpdateRate = 0.8f;
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

            var bloodLoss = internalBloodLoss.BloodLossRatePerStack * internalBloodLoss.Multiplier * stackableComponent.StackCount * UpdateRate;

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
}
