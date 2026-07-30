// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class SiliconPartSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<DamabeableSiliconPartComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnComponentStartup(Entity<DamabeableSiliconPartComponent> ent, ref ComponentStartup args)
    {
        UpdateDamageStatus(ent);
    }

    private void OnComponentShutdown(Entity<DamabeableSiliconPartComponent> ent, ref ComponentShutdown args)
    {
        UpdateDamageStatus(ent);
    }

    private void OnDamageChanged(Entity<DamabeableSiliconPartComponent> ent, ref DamageChangedEvent args)
    {
        UpdateDamageStatus(ent);
    }

    private void UpdateDamageStatus(Entity<DamabeableSiliconPartComponent> ent)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp))
            return;

        if (_damageableSystem.GetTotalDamage(ent.Owner) > ent.Comp.MaxDamageToRemainFunctional && partComp.Active)
        {
            var offlineEv = new SiliconPartStatusOffline(partComp.PartOwner);
            RaiseLocalEvent(ent.Owner, ref offlineEv);
            return;
        }

        if (_damageableSystem.GetTotalDamage(ent.Owner) < ent.Comp.MaxDamageToRemainFunctional && !partComp.Active)
        {
            var onlineEv = new SiliconPartStatusOnline(partComp.PartOwner);
            RaiseLocalEvent(ent.Owner, ref onlineEv);
        }
    }

}

[ByRefEvent]
public record struct SiliconPartStatusOnline(EntityUid? Owner)
{
}

[ByRefEvent]
public record struct SiliconPartStatusOffline(EntityUid? Owner)
{
}

