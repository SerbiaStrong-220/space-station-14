using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Shared.Popups;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

public sealed partial class NarsiCoordinatesRitualEffect : NarsiRitualEffect
{
    public override void MakeRitualEffect(EntityUid altar, EntityUid perfomer, NarsiAltarComponent component, IEntityManager entityManager)
    {
        var popupSystem = entityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
        var target = GetOfferingTarget(entityManager);
        if (target == null)
        {
            popupSystem.PopupEntity("Тёмные силы не смогли достичь цели...", altar, altar, PopupType.Medium);
            return;
        }

        if (!entityManager.TryGetComponent<TransformComponent>(target, out var targetTransform))
            return;

        var pos = targetTransform.MapPosition;
        var x = (int) pos.X;
        var y = (int) pos.Y;

        popupSystem.PopupEntity($"Координаты вашей цели X: {x}; Y: {y}", altar);
    }
}
