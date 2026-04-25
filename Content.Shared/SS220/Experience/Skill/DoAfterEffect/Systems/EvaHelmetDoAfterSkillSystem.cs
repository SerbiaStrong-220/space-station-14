// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Content.Shared.Tag;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class EvaHelmetDoAfterSkillSystem : BaseDoAfterSkillSystem<EvaHelmetDoAfterSkillComponent, ClothingEquipDoAfterEvent>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override bool SkillEffect(Entity<EvaHelmetDoAfterSkillComponent> entity, in DoAfterArgs args)
    {
        if (args.Used is not { } used)
            return false;

        return _tag.HasAnyTag(used, entity.Comp.AffectedTags);
    }
}
