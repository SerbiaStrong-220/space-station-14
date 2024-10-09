using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Server.Medical;
using Content.Server.Nutrition.Components;
using Content.Shared.Popups;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

public sealed partial class NarsiVomitRitualEffect : NarsiRitualEffect
{
    public override void MakeRitualEffect(EntityUid altar, EntityUid perfomer, NarsiAltarComponent component, IEntityManager entityManager)
    {
        var popupSystem = entityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
        var target = GetOfferingTarget(entityManager);
        if (target == null)
        {
            popupSystem.PopupEntity("Цель не найдена", altar, altar, PopupType.Medium);
            return;
        }

        var vomitSys = entityManager.System<VomitSystem>();
        vomitSys.Vomit(target.Value);

        if (!entityManager.TryGetComponent<TransformComponent>(altar, out var altarTransform))
            return;

        var entityLookupSystem = entityManager.System<EntityLookupSystem>();
        var food = entityLookupSystem.GetEntitiesInRange<FoodComponent>(altarTransform.Coordinates, 0.5f);
        if (food.Count > 0)
        {
            popupSystem.PopupEntity("Рядом с алтарем не найдена еда...", altar, altar, PopupType.Medium);
            return;
        }

        var foodEntity = food.First().Owner;
        entityManager.QueueDeleteEntity(foodEntity);
    }
}
