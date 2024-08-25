// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.SyringeGun;

public abstract class SharedSyringeGunSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyringeGunComponent, AttemptShootEvent>(OnAttemptShoot);

        // Events that trigger the removal of temporary components
        SubscribeLocalEvent<TemporarySyringeComponentsComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<TemporarySyringeComponentsComponent, GotEquippedHandEvent>(OnPickup);
    }

    private void OnAttemptShoot(Entity<SyringeGunComponent> ent, ref AttemptShootEvent args)
    {
        var (uid, gunComp) = ent;

        var containerId = gunComp.SyringeContainerId;
        if (containerId is null)
            return;

        if (!_containerSystem.TryGetContainer(uid, containerId, out var container) ||
            container is not ContainerSlot slot)
            return;

        if (slot.ContainedEntity != null)
        {
            var item = (EntityUid)slot.ContainedEntity;
            if (!TryAddTemporaryComponents(ent, item))
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    /// <summary>
    /// Tries to add the necessary components and changes their values if these components did not exist
    /// </summary>
    private bool TryAddTemporaryComponents(EntityUid gun, EntityUid item, SyringeGunComponent? component = null)
    {
        if (!Resolve(gun, ref component))
            return false;

        EnsureComp<TemporarySyringeComponentsComponent>(item, out var temporarySyringeComponents);

        var ev = new AddSyringeTemporaryComponentsEvent(item, false);
        RaiseLocalEvent(gun, ref ev);

        if (ev.Cancelled)
        {
            RemComp<TemporarySyringeComponentsComponent>(item);
            return false;
        }

        if (!EnsureComp<EmbeddableProjectileComponent>(item, out var embeddableProjectile))
        {
            embeddableProjectile.RemovalTime = component.SyringeRemovalTime;
            temporarySyringeComponents.Components.Add(embeddableProjectile.GetType());
            Dirty(item, embeddableProjectile);
        }

        if (!EnsureComp<ThrowingAngleComponent>(item, out var throwingAngle))
        {
            throwingAngle.Angle = component.SyringeThrowAngle;
            temporarySyringeComponents.Components.Add(throwingAngle.GetType());
            Dirty(item, throwingAngle);
        }

        return true;
    }

    /// <summary>
    /// Removes temporary components that were added by <see cref="TryAddTemporaryComponents"/>
    /// </summary>
    public void RemoveTemporaryComponents(EntityUid uid, TemporarySyringeComponentsComponent component)
    {
        foreach (var compType in component.Components)
        {
            RemComp(uid, compType);
        }

        component.Components.Clear();
        RemComp<TemporarySyringeComponentsComponent>(uid);
    }

    private void OnLand(Entity<TemporarySyringeComponentsComponent> ent, ref LandEvent args)
    {
        RemoveTemporaryComponents(ent.Owner, ent.Comp);
    }

    // This method need for situations when someone was able to catch a projectile in flight
    private void OnPickup(Entity<TemporarySyringeComponentsComponent> ent, ref GotEquippedHandEvent args)
    {
        RemoveTemporaryComponents(ent.Owner, ent.Comp);
    }
}

/// <summary>
/// Raised when it is necessary to add new components for the syringe
/// </summary>
/// <param name="Uid"></param>
/// <param name="Cancelled"></param>
[ByRefEvent]
public record struct AddSyringeTemporaryComponentsEvent(EntityUid Uid, bool Cancelled);
