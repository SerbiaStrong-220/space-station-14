// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems.Experience;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

[DataDefinition]
public sealed partial class ApplyDamageAction : ISurgeryGraphEdgeAction
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> DamageGrouped = new();

    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, float>? DamageBonuses;

    [DataField]
    public ProtoId<SkillTreePrototype>? SkillTree;

    [DataField]
    public bool IgnoreResistance = true;

    public void PerformAction(EntityUid uid, EntityUid userUid, EntityUid? used, IEntityManager entityManager)
    {
        var modifier = 1f;
        if (DamageBonuses is not null && SkillTree is not null)
        {
            var experienceSystem = entityManager.System<ExperienceSystem>();
            if (experienceSystem.TryGetSkillTreeLevel(userUid, SkillTree, out var skillProto) &&
                DamageBonuses.TryGetValue(skillProto.Value, out var skillModifier))
            {
                modifier = skillModifier;
            }
        }

        var damagableSystem = entityManager.System<DamageableSystem>();

        // a surgery step is not a hit, its damage doesn't roll traumas
        entityManager.System<SharedPathologySystem>().TryChangeDamageWithoutTraumas(uid, Damage * modifier, out _, IgnoreResistance, userUid);

        foreach (var (group, amount) in DamageGrouped)
        {
            damagableSystem.HealEvenly(uid, amount * modifier, group);
        }
    }
}
