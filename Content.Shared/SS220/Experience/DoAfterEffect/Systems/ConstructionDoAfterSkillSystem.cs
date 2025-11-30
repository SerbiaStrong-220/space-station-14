// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Construction;
using Content.Shared.Medical;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Content.Shared.Stacks;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class ConstructionDoAfterSkillSystem : BaseDoAfterSkillSystem<ConstructionDoAfterSkillComponent, ConstructionInteractDoAfterEvent>
{
    protected override void OnDoAfterEnd(Entity<ConstructionDoAfterSkillComponent> entity, ref DoAfterBeforeComplete args)
    {
        if (args.Args.Used is null)
            return;

        if (!TryComp<StackComponent>(args.Args.Used, out var stack))
            return;

        if (entity.Comp.ComplexMaterials.Contains(stack.StackTypeId));

        base.OnDoAfterEnd(entity, ref args);
    }
}
