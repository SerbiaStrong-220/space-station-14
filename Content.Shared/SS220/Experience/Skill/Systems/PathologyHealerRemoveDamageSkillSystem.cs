// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Pathology;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class PathologyHealerRemoveDamageSkillSystem : SkillEntitySystem
{
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
    }
}
