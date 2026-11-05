// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class PathologyHealerRemoveDamageSkillSystem : SkillEntitySystem
{
    private static readonly ProtoId<SkillTreePrototype> AffectedSkillTree = "Anatomy";

    private static readonly FixedPoint4 ProgressForUse = 0.04;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<PathologyHealerRemoveDamageSkillComponent, GetPathologyHealerDamageModifier>(OnGetPathologyHealerDamageModifier);
    }

    private void OnGetPathologyHealerDamageModifier(Entity<PathologyHealerRemoveDamageSkillComponent> entity, ref GetPathologyHealerDamageModifier args)
    {
        if (!entity.Comp.RemoveDamageModifier.TryGetValue(args.PathologyId, out var modifier))
            return;

        args.Modifier *= modifier;
        TryChangeStudyingProgress(entity, AffectedSkillTree, ProgressForUse);
    }
}
