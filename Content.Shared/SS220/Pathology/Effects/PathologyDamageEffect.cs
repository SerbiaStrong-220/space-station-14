// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>
/// Applies periodic damage while the pathology is active (for example, burn necrosis toxin damage).
/// </summary>
public sealed partial class PathologyDamageEffect : IPathologyEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        if (entityManager.System<MobStateSystem>().IsDead(uid))
            return;

        var damageable = entityManager.System<DamageableSystem>();
        damageable.TryChangeDamage(uid, Damage * data.StackCount, ignoreResistances: true, interruptsDoAfters: false);
    }
}
