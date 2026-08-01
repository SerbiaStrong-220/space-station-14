// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
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

    private static readonly string PartContainerPrefix = "silicon_component";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DamabeableSiliconPartComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<SiliconComponentsComponent, CanSeeAttemptEvent>(OnCanSeeCheck);

        SubscribeLocalEvent<ActiveOpticsComponent, ComponentGotInsertedIntoUser>(OnOpticsInserted);
        SubscribeLocalEvent<ActiveOpticsComponent, ComponentGotRemovedFromUser>(OnOpticsRemoved);
        SubscribeLocalEvent<ActiveOpticsComponent, DamageChangedEvent>(OnOpticsDamageChanged);

        SubscribeLocalEvent<BrainComponent, ComponentGotInsertedIntoUser>(OnBrainInserted);
        SubscribeLocalEvent<BrainComponent, ComponentGotRemovedFromUser>(OnBrainRemoved);

        SubscribeLocalEvent<SiliconPartComponent, EntityUnvisitedEvent>(OnMindVisited);

        SubscribeLocalEvent<SiliconPartComponent, MindAddedMessage>(OnBrainMindAdded);
        SubscribeLocalEvent<SiliconComponentsComponent, MindAddedMessage>(OnSiliconMindAdded);

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
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp))
            return;

        if (TryComp<AntagGearRelayComponent>(ent.Owner, out var antagGearRelay))
            antagGearRelay.User = null;

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind) &&
            _player.TryGetSessionById(mind.UserId, out var session))
            _mind.UnVisit(mindId);


        if (!TryComp<ExperienceComponent>(ent.Owner, out var brainExperience))
            return;

        if (_net.IsClient)
            return;

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

        return;
    }

    private void OnMindVisited(Entity<SiliconPartComponent> ent, ref EntityUnvisitedEvent args)
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

