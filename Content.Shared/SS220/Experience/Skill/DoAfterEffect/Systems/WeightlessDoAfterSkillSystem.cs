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
    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<WeightlessDoAfterSkillComponent, WeightlessnessChangedEvent>(OnWeightlessChange);
    }

    protected override bool SkillEffect(Entity<WeightlessDoAfterSkillComponent> entity, in DoAfterArgs args)
    {
        return entity.Comp.Weightless;
    }

    private void OnWeightlessChange(Entity<WeightlessDoAfterSkillComponent> entity, ref WeightlessnessChangedEvent args)
    {
        entity.Comp.Weightless = args.Weightless;
    }
}
