// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Gravity;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class WeightlessDoAfterSkillSystem : BaseDoAfterSkillSystem<WeightlessDoAfterSkillComponent, DoAfterEvent>
{
    private readonly ProtoId<SkillTreePrototype> _affectedSkillTree = "ExtravehicularActivity";
    private readonly FixedPoint4 _learningReward = 0.07f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<WeightlessDoAfterSkillComponent, WeightlessnessChangedEvent>(OnWeightlessChange);
    }

    protected override bool SkillEffect(Entity<WeightlessDoAfterSkillComponent> entity, in DoAfterArgs args)
    {
        return entity.Comp.Weightless;
    }

    protected override void AfterDoAfterComplete(Entity<WeightlessDoAfterSkillComponent> entity, in BeforeDoAfterCompleteEvent args)
    {
        base.AfterDoAfterComplete(entity, args);

        TryChangeStudyingProgress(entity, _affectedSkillTree, _learningReward);
    }

    private void OnWeightlessChange(Entity<WeightlessDoAfterSkillComponent> entity, ref WeightlessnessChangedEvent args)
    {
        entity.Comp.Weightless = args.Weightless;
    }
}
