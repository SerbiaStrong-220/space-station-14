using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

public sealed partial class NarsiSummonTargetRitualEffect : NarsiRitualEffect
{
    public override void MakeRitualEffect(EntityUid altar, EntityUid perfomer, NarsiAltarComponent component, IEntityManager entityManager)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var popupSystem = entityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();

        if (random.Next(1, 100) > 15)
        {
            popupSystem.PopupEntity("Нас постигла неудача...", altar, altar, PopupType.Medium);
            return;
        }

        var target = GetOfferingTarget(entityManager);
        if (target == null)
        {
            popupSystem.PopupEntity("Тёмные силы не смогли достичь цели...", altar, altar, PopupType.Medium);
            return;
        }

        if (!entityManager.TryGetComponent<TransformComponent>(altar, out var transform))
            return;

        var transformSystem = entityManager.System<TransformSystem>();
        transformSystem.SetCoordinates(target.Value, transform.Coordinates);
        transformSystem.AttachToGridOrMap(target.Value);
    }
}
