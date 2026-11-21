using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Content.Shared.Rejuvenate;
using Content.Shared.SS220.DarkForces.Narsi.Roles;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

[DataDefinition]
public sealed partial class NarsiHealRitualEffect : NarsiRitualEffect
{
    public override void MakeRitualEffect(EntityUid altar, EntityUid perfomer, NarsiAltarComponent component,
        IEntityManager entityManager)
    {
        var cultists = entityManager.EntityQueryEnumerator<NarsiCultistComponent>();
        while (cultists.MoveNext(out var cultist, out _))
        {
            entityManager.EventBus.RaiseLocalEvent(cultist, new RejuvenateEvent());
        }

        if (component.BuckledEntity == null)
            return;

        entityManager.QueueDeleteEntity(component.BuckledEntity.Value);
        component.BuckledEntity = null;
    }
}
