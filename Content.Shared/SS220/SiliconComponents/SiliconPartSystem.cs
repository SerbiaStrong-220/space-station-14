// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class SiliconPartSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<SiliconComponentsComponent, CanSeeAttemptEvent>(OnCanSeeCheck);

        SubscribeLocalEvent<ActiveOpticsComponent, ComponentGotInsertedIntoUser>(OnOpticsInserted);
        SubscribeLocalEvent<ActiveOpticsComponent, ComponentGotRemovedFromUser>(OnOpticsRemoved);
        SubscribeLocalEvent<ActiveOpticsComponent, DamageChangedEvent>(OnOpticsDamageChanged);

        SubscribeLocalEvent<ActiveOpticsComponent, SiliconPartStatusOnline>(OnPartOnline);
        SubscribeLocalEvent<ActiveOpticsComponent, SiliconPartStatusOffline>(OnPartOffline);

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

    private void OnCanSeeCheck(Entity<SiliconComponentsComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!ent.Comp.Parts.TryGetValue(PartType.Optics, out var opticsContainer) || opticsContainer.ContainedEntity is not { Valid: true } opticsValidated)
        {
            args.Cancel();
            return;
        }

        if (!HasComp<ActiveOpticsComponent>(opticsValidated) || TryComp<SiliconPartComponent>(ent.Owner, out var partComp) && !partComp.Active)
        {
            args.Cancel();
            return;
        }

        if (TryComp<DamabeableSiliconPartComponent>(opticsValidated, out var damageablePartComp) &&
            TryComp<DamageableComponent>(opticsValidated, out var damageableComp) &&
            TryComp<BlindableComponent>(ent.Owner, out var blindableComp))
        {
            _blindable.AdjustEyeDamage(ent.Owner, FixedPoint2.Clamp(_damageableSystem.GetTotalDamage(ent.Owner) / damageablePartComp.MaxDamageToRemainFunctional * 9, 0, 9).Int() - blindableComp.EyeDamage);
        }
    }

    private void OnOpticsInserted(Entity<ActiveOpticsComponent> ent, ref ComponentGotInsertedIntoUser args)
    {
        if (!HasComp<SiliconComponentsComponent>(args.Owner))
            return;

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || args.Owner is not { Valid: true } || !partComp.Active)
            return;

        if (!TryComp<BlindableComponent>(args.Owner, out var ownerBlindableComp))
            return;

        _blindable.UpdateIsBlind(args.Owner);
    }

    private void OnOpticsRemoved(Entity<ActiveOpticsComponent> ent, ref ComponentGotRemovedFromUser args)
    {
        if (!TryComp<SiliconComponentsComponent>(args.Owner, out var ownerComp))
            return;

        _blindable.UpdateIsBlind(args.Owner);
    }

    private void OnPartOnline(Entity<ActiveOpticsComponent> ent, ref SiliconPartStatusOnline args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated)
            return;

        _blindable.UpdateIsBlind(ownerValidated);
    }

    private void OnPartOffline(Entity<ActiveOpticsComponent> ent, ref SiliconPartStatusOffline args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated)
            return;

        _blindable.UpdateIsBlind(ownerValidated);
    }

    private void OnOpticsDamageChanged(Entity<ActiveOpticsComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValid)
            return;

        if (!HasComp<SiliconComponentsComponent>(ownerValid))
            return;

        _blindable.UpdateIsBlind(ownerValid);
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

