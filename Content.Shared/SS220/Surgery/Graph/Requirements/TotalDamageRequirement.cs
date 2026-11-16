// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems.Experience;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class TotalDamageRequirement : SurgeryGraphRequirement
{
    [DataField]
    public ProtoId<DamageTypePrototype>? DamageType;

    [DataField]
    public ProtoId<DamageGroupPrototype>? DamageGroup;

    [DataField(required: true)]
    public FixedPoint2 Damage;

    [DataField]
    public ProtoId<SkillTreePrototype>? SkillTree;

    [DataField]
    public Dictionary<ProtoId<SkillPrototype>, float>? DamageMultipliers;

    protected override bool Requirement(EntityUid? uid, EntityUid user, IEntityManager entityManager)
    {
        var damageSystem = entityManager.System<DamageableSystem>();
        if (!entityManager.TryGetComponent<DamageableComponent>(uid, out _))
            return false;

        var modifier = 1f;
        if (DamageMultipliers is not null && SkillTree is not null)
        {
            var experienceSystem = entityManager.System<ExperienceSystem>();
            _ = experienceSystem.TryGetSkillTreeLevel(user, SkillTree, out var skillProto)
                && DamageMultipliers.TryGetValue(skillProto.Value, out modifier);
        }

        if (DamageGroup is not null)
        {
            var perGroup = damageSystem.GetDamagePerGroup(uid.Value);
            var groupDamage = perGroup.TryGetValue(DamageGroup.Value, out var value) ? value : FixedPoint2.Zero;
            return groupDamage > Damage * modifier;
        }

        if (DamageType is not null)
        {
            var allDamage = damageSystem.GetAllDamage(uid.Value);
            return allDamage.DamageDict.TryGetValue(DamageType.Value, out var typeDamage) && typeDamage > Damage * modifier;
        }

        return damageSystem.GetTotalDamage(uid.Value) > Damage * modifier;
    }
}
