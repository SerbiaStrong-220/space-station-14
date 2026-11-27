using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Content.Server.Mind;
using Content.Shared.SS220.DarkForces.Narsi.Roles;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

public sealed partial class NarsiChangeMindRitualEffect : NarsiRitualEffect
{

    public override void MakeRitualEffect(EntityUid altar, EntityUid perfomer, NarsiAltarComponent component, IEntityManager entityManager)
    {
        var buckledEntity = component.BuckledEntity;
        if (buckledEntity == null)
            return;

        var mindSystem = entityManager.System<MindSystem>();

        if (!mindSystem.TryGetMind(perfomer, out var performerMindId, out _))
            return;

        if (!mindSystem.TryGetMind(buckledEntity.Value, out var targetMindId, out _))
            return;

        mindSystem.TransferTo(performerMindId, buckledEntity);
        mindSystem.TransferTo(targetMindId, perfomer);

        //Если культисты делают что-то между собой, нет трогаем
        if (entityManager.HasComponent<NarsiCultistComponent>(buckledEntity))
            return;

        //Если культисты меняются разумом с обычным человеком, убираем у старой сущности компонент культиста и добавляем новой.
        entityManager.RemoveComponent<NarsiCultistComponent>(perfomer);
        entityManager.AddComponent<NarsiCultistComponent>(buckledEntity.Value);
    }
}
