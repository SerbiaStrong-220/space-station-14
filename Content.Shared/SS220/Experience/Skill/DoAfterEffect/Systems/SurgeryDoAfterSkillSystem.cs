// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Content.Shared.SS220.Surgery;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed partial class SurgeryDoAfterSkillSystem : BaseDoAfterSkillSystem<SurgeryDoAfterSkillComponent, SurgeryDoAfterEvent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<SurgeryDoAfterSkillComponent, GetSurgeryDelayModifiersEvent>(OnGetDelayModifiers);
        SubscribeEventToSkillEntity<SurgeryDoAfterSkillComponent, GetSurgeryFailureChanceEvent>(OnGetFailureChance);
    }

    private void OnGetDelayModifiers(Entity<SurgeryDoAfterSkillComponent> entity, ref GetSurgeryDelayModifiersEvent args)
    {
        args.Multiplier *= entity.Comp.DurationScale;
    }

    protected override void OnDoAfterStart(Entity<SurgeryDoAfterSkillComponent> entity, ref BeforeDoAfterStartEvent args)
    {
        // DurationScale is applied via GetSurgeryDelayModifiersEvent in SharedSurgerySystem.GetStepDelay
        // so DeceaseSkillBonus and minimum delay clamping apply cleanly.
        if (entity.Comp.FullBlock)
            base.OnDoAfterStart(entity, ref args);
    }

    private void OnGetFailureChance(Entity<SurgeryDoAfterSkillComponent> entity, ref GetSurgeryFailureChanceEvent args)
    {
        args.FlatModifier += entity.Comp.FailureChance;
    }

    protected override void OnDoAfterEnd(Entity<SurgeryDoAfterSkillComponent> entity, ref BeforeDoAfterCompleteEvent args)
    {
        // Failure roll is handled unified by SharedSurgerySystem alongside anesthesia, table, and tool modifiers.
    }
}
