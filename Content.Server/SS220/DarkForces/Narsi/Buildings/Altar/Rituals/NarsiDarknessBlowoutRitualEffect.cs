using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Content.Server.Polymorph.Systems;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Humanoid;
using Content.Shared.Polymorph;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

[DataDefinition]
public sealed partial class NarsiDarknessBlowoutRitualEffect : NarsiRitualEffect
{
    [DataField(required: true)]
    public PolymorphConfiguration Configuration = default!;

    public override void MakeRitualEffect(EntityUid altar, EntityUid perfomer, NarsiAltarComponent component, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<TransformComponent>(altar, out var altarTransform))
            return;

        var polymorphSystem = entityManager.EntitySysManager.GetEntitySystem<PolymorphSystem>();
        var query = entityManager.EntityQueryEnumerator<HumanoidAppearanceComponent, RottingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var entTransform))
        {
            if (entTransform.MapID != altarTransform.MapID)
                continue;

            polymorphSystem.PolymorphEntity(uid, Configuration);
        }
    }
}
