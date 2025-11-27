// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class SkillMedicineDoAfterSystem : BaseSkillDoAfterEffectSystem<SkillMedicineDoAfterComponent, HealingDoAfterEvent>
{
    private readonly ProtoId<SkillTreePrototype> _skillTreeGroup = "Medicine";

    protected override void OnDoAfterStart(Entity<SkillMedicineDoAfterComponent> entity, ref BeforeDoAfterStartEvent args)
    {
        base.OnDoAfterStart(entity, ref args);

        if (!Experience.TryGetExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
        {
            Log.Error($"Cant get owner of skill entity {ToPrettyString(entity)}");
            return;
        }

        Experience.TryChangeStudyingProgress(experienceEntity.Value.Owner, _skillTreeGroup, 0.6f);
    }

    protected override void OnDoAfterEnd(Entity<SkillMedicineDoAfterComponent> entity, ref DoAfterBeforeComplete args)
    {
        base.OnDoAfterEnd(entity, ref args);

        if (!Experience.TryGetExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
        {
            Log.Error($"Cant get owner of skill entity {ToPrettyString(entity)}");
            return;
        }

        Experience.TryChangeStudyingProgress(experienceEntity.Value.Owner, _skillTreeGroup, 0.6f);
    }

}
