// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class ApplyDamageAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

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
            _ = experienceSystem.TryGetSkillTreeLevel(userUid, SkillTree, out var skillProto)
                && DamageBonuses.TryGetValue(skillProto.Value, out modifier);
        }

        entityManager.System<DamageableSystem>().TryChangeDamage(uid, Damage * modifier, IgnoreResistance, origin: userUid);
    }
}
