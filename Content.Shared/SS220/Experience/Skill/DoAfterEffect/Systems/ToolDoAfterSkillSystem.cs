// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class ToolDoAfterSkillSystem : BaseDoAfterSkillSystem<ConstructionToolDoAfterSkillComponent, SharedToolSystem.ToolDoAfterEvent>
{
    private const float MinimumDoAfterDelayToLearn = 3f;

    protected override void OnDoAfterEnd(Entity<ConstructionToolDoAfterSkillComponent> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (args.Args.Used is null)
            return;

        base.OnDoAfterEnd(entity, ref args);
    }

    protected override bool LearnedAfterComplete(in BeforeDoAfterCompleteEvent args)
    {
        return args.Args.Delay.TotalSeconds > MinimumDoAfterDelayToLearn;
    }
}

