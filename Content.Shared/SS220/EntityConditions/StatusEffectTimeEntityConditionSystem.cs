// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityConditions;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.EntityConditions;

/// <summary>
/// Passes when the status effect has at least <see cref="StatusEffectTimeCondition.Min"/> time left,
/// e.g. heavy drunkenness damaging the liver.
/// </summary>
public sealed partial class StatusEffectTimeEntityConditionSystem : EntityConditionSystem<StatusEffectContainerComponent, StatusEffectTimeCondition>
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected override void Condition(Entity<StatusEffectContainerComponent> entity, ref EntityConditionEvent<StatusEffectTimeCondition> args)
    {
        if (!_statusEffects.TryGetTime(entity, args.Condition.Effect, out var time, entity.Comp))
            return;

        // no end time means the effect lasts forever
        args.Result = time.EndEffectTime is not { } endTime || endTime - _timing.CurTime >= args.Condition.Min;
    }
}

public sealed partial class StatusEffectTimeCondition : EntityConditionBase<StatusEffectTimeCondition>
{
    [DataField(required: true)]
    public EntProtoId Effect;

    [DataField(required: true)]
    public TimeSpan Min;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var name = prototype.Resolve(Effect, out var effectPrototype) ? effectPrototype.Name : Effect.Id;
        return Loc.GetString("entity-condition-guidebook-ss220-status-effect-time", ("effect", name), ("seconds", (int) Min.TotalSeconds));
    }
}
