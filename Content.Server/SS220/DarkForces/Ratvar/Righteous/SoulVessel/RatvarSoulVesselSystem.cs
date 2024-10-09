using Content.Server.Mind;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkForces.Ratvar.Events;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Items;
using Content.Shared.Silicons.Borgs.Components;
using RatvarSoulVesselDoAfterEvent = Content.Shared.SS220.DarkForces.Ratvar.DoAfterEvents.RatvarSoulVesselDoAfterEvent;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.SoulVessel;

public sealed class RatvarSoulVesselSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarSoulVesselComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<RatvarSoulVesselComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RatvarSoulVesselComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RatvarSoulVesselComponent, RatvarSoulVesselDoAfterEvent>(OnDoAfterEvent);
    }

    private void OnDoAfterEvent(EntityUid uid, RatvarSoulVesselComponent component, RatvarSoulVesselDoAfterEvent args)
    {
        var target = args.Target;
        if (target == null || args.Cancelled || args.Handled)
            return;

        if (!_mindSystem.TryGetMind(target.Value, out var mindId, out var mind))
            return;

        if (mind.IsVisitingEntity)
            _mindSystem.UnVisit(mindId);

        _mindSystem.TransferTo(mindId, uid);
    }

    private void OnAfterInteract(EntityUid uid, RatvarSoulVesselComponent component, AfterInteractEvent args)
    {
        var target = args.Target;
        if (args.Handled || target == null)
            return;

        var isHumanoidOrBorg = IsHumanoidOrBorg(target.Value);
        var isMindValid = IsMindValid(target.Value);

        if (!isHumanoidOrBorg || !isMindValid)
            return;

        var doAfterEv = new RatvarSoulVesselDoAfterEvent();
        var doAfterEvArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            TimeSpan.FromSeconds(8),
            eventTarget: uid,
            target: target,
            @event: doAfterEv
        )
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 1.5f
        };

        _doAfterSystem.TryStartDoAfter(doAfterEvArgs);
        args.Handled = true;
    }

    private bool IsHumanoidOrBorg(EntityUid uid)
    {
        return HasComp<HumanoidAppearanceComponent>(uid) || HasComp<BorgChassisComponent>(uid);
    }

    private bool IsMindValid(EntityUid uid)
    {
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) ||
            !TryComp<MindComponent>(mindContainer.Mind, out var mind))
            return false;

        return mind is { UserId: not null, Session: not null };
    }

    private void OnMindRemoved(EntityUid uid, RatvarSoulVesselComponent component, MindRemovedMessage args)
    {
        _appearanceSystem.SetData(uid, RatvarSoulVesselVisualState.State, false);
    }

    private void OnMindAdded(EntityUid uid, RatvarSoulVesselComponent component, MindAddedMessage args)
    {
        _appearanceSystem.SetData(uid, RatvarSoulVesselVisualState.State, true);
    }
}
