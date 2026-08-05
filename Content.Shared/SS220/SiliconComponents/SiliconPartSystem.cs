// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Mind;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class SiliconPartSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    private static readonly string PartContainerPrefix = "silicon_component";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<SiliconComponentsComponent, CanSeeAttemptEvent>(OnCanSeeCheck);

        SubscribeLocalEvent<SiliconComponentsComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);

        SubscribeLocalEvent<ActiveOpticsComponent, ComponentGotInsertedIntoUser>(OnOpticsInserted);
        SubscribeLocalEvent<ActiveOpticsComponent, ComponentGotRemovedFromUser>(OnOpticsRemoved);

        SubscribeLocalEvent<BrainComponent, ComponentGotInsertedIntoUser>(OnBrainInserted);
        SubscribeLocalEvent<BrainComponent, ComponentGotRemovedFromUser>(OnBrainRemoved);

        SubscribeLocalEvent<ActiveOpticsComponent, SiliconPartStatusOnline>(OnPartOnline);
        SubscribeLocalEvent<ActiveOpticsComponent, SiliconPartStatusOffline>(OnPartOffline);

        SubscribeLocalEvent<ActiveOpticsComponent, SiliconPartDamageModifierChanged>(OnPartDamageModChanged);

        SubscribeLocalEvent<MovementSpeedModifyingPartComponent, SiliconPartStatusOnline>(OnMovementModifierOnline);
        SubscribeLocalEvent<MovementSpeedModifyingPartComponent, SiliconPartStatusOffline>(OnMovementModifierOffline);

        SubscribeLocalEvent<MovementSpeedModifyingPartComponent, ComponentGotInsertedIntoUser>(OnMovementModifierInserted);
        SubscribeLocalEvent<MovementSpeedModifyingPartComponent, ComponentGotRemovedFromUser>(OnMovementModifierRemoved);

        SubscribeLocalEvent<SiliconPartComponent, EntityUnvisitConpleteEvent>(OnMindReturnedToBrain);

        SubscribeLocalEvent<SiliconPartComponent, MindAddedMessage>(OnBrainMindAdded);
        SubscribeLocalEvent<SiliconComponentsComponent, MindAddedMessage>(OnSiliconMindAdded);

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

        if (TryGetIntegrityModifier(ent.AsNullable(), out FixedPoint2 modifier) &&
            ent.Comp.CurrentDamageEfficiencyModifier != modifier)
        {
            ent.Comp.CurrentDamageEfficiencyModifier = modifier;

            var damageModEvent = new SiliconPartDamageModifierChanged(modifier);
            RaiseLocalEvent(ent.Owner, ref damageModEvent);
        }

        if (_damageableSystem.GetTotalDamage(ent.Owner) > ent.Comp.MaxDamageToRemainFunctional && partComp.Active)
        {
            var offlineEv = new SiliconPartStatusOffline(partComp.PartOwner);
            RaiseLocalEvent(ent.Owner, ref offlineEv);

            partComp.Active = false;
            Dirty(ent);

            return;
        }

        if (_damageableSystem.GetTotalDamage(ent.Owner) < ent.Comp.MaxDamageToRemainFunctional && !partComp.Active)
        {
            var onlineEv = new SiliconPartStatusOnline(partComp.PartOwner);
            RaiseLocalEvent(ent.Owner, ref onlineEv);

            partComp.Active = true;
            Dirty(ent);

            return;
        }

        Dirty(ent);
    }

    private void OnCanSeeCheck(Entity<SiliconComponentsComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!ent.Comp.Online)
        {
            args.Cancel();
            return;
        }

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

    private void OnRefreshMovementSpeed(Entity<SiliconComponentsComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var part in ent.Comp.Parts.Values)
        {
            if (!TryComp<MovementSpeedModifyingPartComponent>(part.ContainedEntity, out var speedModComp) ||
                !TryComp<SiliconPartComponent>(part.ContainedEntity, out var partComp) || !partComp.Active && speedModComp.RequiresActive)
                continue;

            args.ModifySpeed(speedModComp.SpeedMod.SprintSpeedModifier, speedModComp.SpeedMod.WalkSpeedModifier);
        }
    }

    private void OnMovementModifierInserted(Entity<MovementSpeedModifyingPartComponent> ent, ref ComponentGotInsertedIntoUser args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnMovementModifierRemoved(Entity<MovementSpeedModifyingPartComponent> ent, ref ComponentGotRemovedFromUser args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnMovementModifierOnline(Entity<MovementSpeedModifyingPartComponent> ent, ref SiliconPartStatusOnline args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnMovementModifierOffline(Entity<MovementSpeedModifyingPartComponent> ent, ref SiliconPartStatusOffline args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnBrainInserted(Entity<BrainComponent> ent, ref ComponentGotInsertedIntoUser args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp))
            return;

        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (partComp.PartOwner is not { Valid: true } ownerValidated)
            return;

        if (!TryComp<SiliconComponentsComponent>(ownerValidated, out var siliconComp) ||
            container.ID != PartContainerPrefix + "_" + PartType.Brain)
            return;

        if (TryComp<AntagGearRelayComponent>(ent.Owner, out var antagGearRelay))
            antagGearRelay.User = ownerValidated;

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session))
            _mind.Visit(mindId, ownerValidated, mind: mind);

        if (HasComp<ExperienceComponent>(ent.Owner) && HasComp<ExperienceComponent>(ownerValidated))
        {
            if (_net.IsClient)
                return;

            if (TryComp<AdminForcedExperienceAddComponent>(ent.Owner, out var brainAdminExperience)) // I feel guilty for creating this
            {
                var userAdminExperience = EnsureComp<AdminForcedExperienceAddComponent>(ownerValidated);

                _entManager.CopyComponent(ent.Owner, args.Owner, brainAdminExperience);

                RemComp<AdminForcedExperienceAddComponent>(ent.Owner);
            }
            if (TryComp<RoleExperienceAddComponent>(ent.Owner, out var brainRoleExperience))
            {
                var userRoleExperience = EnsureComp<RoleExperienceAddComponent>(ownerValidated);

                _entManager.CopyComponent(ent.Owner, args.Owner, brainRoleExperience);

                RemComp<RoleExperienceAddComponent>(ent.Owner);
            }
            if (TryComp<JobBackgroundSublevelAddComponent>(ent.Owner, out var brainBackgroundExperience))
            {
                var userBackgroundExperience = EnsureComp<JobBackgroundSublevelAddComponent>(ownerValidated);

                _entManager.CopyComponent(ent.Owner, args.Owner, brainBackgroundExperience);

                RemComp<JobBackgroundSublevelAddComponent>(ent.Owner);
            }

            var afterGainedEv = new RecalculateEntityExperience();
            RaiseLocalEvent(ent.Owner, ref afterGainedEv);
            RaiseLocalEvent(ownerValidated, ref afterGainedEv);

            return;

        }

        EnsureComp<ExperienceComponent>(ent.Owner);
    }

    private void OnBrainRemoved(Entity<BrainComponent> ent, ref ComponentGotRemovedFromUser args)
    {
        if (TryComp<AdminForcedExperienceAddComponent>(args.Owner, out var userAdminExperience))
        {
            var brainAdminExperience = EnsureComp<AdminForcedExperienceAddComponent>(ent.Owner);

            _entManager.CopyComponent(args.Owner, ent.Owner, userAdminExperience);

            RemComp<AdminForcedExperienceAddComponent>(args.Owner);
        }
        if (TryComp<RoleExperienceAddComponent>(args.Owner, out var userRoleExperience))
        {
            var brainRoleExperience = EnsureComp<RoleExperienceAddComponent>(ent.Owner);

            _entManager.CopyComponent(args.Owner, ent.Owner, userRoleExperience);

            RemComp<RoleExperienceAddComponent>(args.Owner);
        }
        if (TryComp<JobBackgroundSublevelAddComponent>(args.Owner, out var userBackgroundExperience))
        {
            var brainBackgroundExperience = EnsureComp<JobBackgroundSublevelAddComponent>(ent.Owner);

            _entManager.CopyComponent(args.Owner, ent.Owner, userBackgroundExperience);

            RemComp<JobBackgroundSublevelAddComponent>(args.Owner);
        }

        var afterGainedEv = new RecalculateEntityExperience();
        RaiseLocalEvent(ent.Owner, ref afterGainedEv);
        RaiseLocalEvent(args.Owner, ref afterGainedEv);

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp))
            return;

        if (TryComp<AntagGearRelayComponent>(ent.Owner, out var antagGearRelay))
            antagGearRelay.User = null;

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session) &&
            TryComp<VisitingMindComponent>(args.Owner, out var ownerVisitComp) &&
            ownerVisitComp.MindId == mindId)
            _mind.UnVisit(mindId);
    }

    private void OnMindReturnedToBrain(Entity<SiliconPartComponent> ent, ref EntityUnvisitConpleteEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (ent.Comp.PartOwner is not { Valid: true } ownerValidated)
            return;

        if (!TryComp<SiliconComponentsComponent>(ownerValidated, out var siliconComp) ||
            container.ID != PartContainerPrefix + "_" + PartType.Brain)
            return;

        if (TryComp<AntagGearRelayComponent>(ent.Owner, out var antagGearRelay))
            antagGearRelay.User = ownerValidated;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind) ||
            !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _mind.Visit(mindId, ownerValidated, mind: mind);
    }

    private void OnBrainMindAdded(Entity<SiliconPartComponent> ent, ref MindAddedMessage args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (ent.Comp.PartOwner is not { Valid: true } ownerValidated)
            return;

        if (!TryComp<SiliconComponentsComponent>(ownerValidated, out var siliconComp) ||
            container.ID != PartContainerPrefix + "_" + PartType.Brain)
            return;

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session))
            _mind.Visit(mindId, ownerValidated, mind: mind);

        if (TryComp<ExperienceComponent>(ent.Owner, out var brainExperience))
            return;

        brainExperience = EnsureComp<ExperienceComponent>(ent.Owner);
    }

    private void OnSiliconMindAdded(Entity<SiliconComponentsComponent> ent, ref MindAddedMessage args)
    {
        if (!ent.Comp.Parts.TryGetValue(PartType.Brain, out var brainContainer))
            return;

        if (brainContainer.ContainedEntity is not { Valid: true } brainValidated || !HasComp<BrainComponent>(brainValidated))
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind) ||
            !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _mind.TransferTo(mindId, brainValidated, mind: mind);
    }

    private void OnOpticsInserted(Entity<ActiveOpticsComponent> ent, ref ComponentGotInsertedIntoUser args)
    {
        if (!HasComp<SiliconComponentsComponent>(args.Owner))
            return;

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || args.Owner is not { Valid: true } || !partComp.Active)
            return;

        if (TryComp<DamabeableSiliconPartComponent>(args.Owner, out var damageablePartComp))
            if (TryComp<BlindableComponent>(args.Owner, out var ownerBlindableComp))
                _blindable.AdjustEyeDamage(args.Owner, Math.Max(ent.Comp.EyeDamage, (ownerBlindableComp.MaxDamage * damageablePartComp.CurrentDamageEfficiencyModifier).Int()) - ownerBlindableComp.EyeDamage);

        _blindable.UpdateIsBlind(args.Owner);
    }

    private void OnOpticsRemoved(Entity<ActiveOpticsComponent> ent, ref ComponentGotRemovedFromUser args)
    {
        if (!TryComp<SiliconComponentsComponent>(args.Owner, out var ownerComp))
            return;

        if (TryComp<BlindableComponent>(args.Owner, out var ownerBlindableComp))
            _blindable.AdjustEyeDamage(args.Owner, -ownerBlindableComp.EyeDamage);

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

    private void OnPartDamageModChanged(Entity<ActiveOpticsComponent> ent, ref SiliconPartDamageModifierChanged args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated)
            return;

        if (!TryComp<BlindableComponent>(ownerValidated, out var blindableComp))
            return;

        _blindable.AdjustEyeDamage(ownerValidated, (blindableComp.MaxDamage * args.Modifier).Int() - blindableComp.EyeDamage);

        _blindable.UpdateIsBlind(ownerValidated);
    }

    public bool TryGetIntegrityModifier(Entity<DamabeableSiliconPartComponent?> part, out FixedPoint2 modifier)
    {
        modifier = 1;

        if (!Resolve(part.Owner, ref part.Comp))
            return false;

        modifier = FixedPoint2.Clamp(
            (_damageableSystem.GetTotalDamage(part.Owner) - part.Comp.MinDamageToMalfunction) /
            (part.Comp.MaxDamageToRemainFunctional - part.Comp.MinDamageToMalfunction),
            0,
            1);

        return true;
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

[ByRefEvent]
public record struct SiliconPartDamageModifierChanged(FixedPoint2 Modifier)
{
}

