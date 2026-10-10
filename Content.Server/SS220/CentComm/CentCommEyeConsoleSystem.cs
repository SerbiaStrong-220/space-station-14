// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Shuttles.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.CentComm;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.SS220.CentComm;

/// <summary>
/// Lets the console operator pilot a remote eye over the station.
/// The operator's mind visits the eye and returns on demand.
/// </summary>
public sealed partial class CentCommEyeConsoleSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private ViewSubscriberSystem _viewSubscriber = default!;

    private static readonly EntProtoId ReturnAction = "ActionCentCommEyeReturn";
    private static readonly EntProtoId ChannelsAction = "ActionToggleRadioChannelsUI";

    public override void Initialize()
    {
        base.Initialize();

        // Console
        SubscribeLocalEvent<CentCommEyeConsoleComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CentCommEyeConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CentCommEyeConsoleComponent, ComponentShutdown>(OnConsoleShutdown);

        // Eye
        SubscribeLocalEvent<CentCommEyeUserComponent, CentCommEyeReturnActionEvent>(OnReturnAction);
        SubscribeLocalEvent<CentCommEyeUserComponent, PlayerDetachedEvent>(OnEyeDetached);
        SubscribeLocalEvent<CentCommEyeUserComponent, EntityTerminatingEvent>(OnEyeTerminating);

        // Body left behind at the console
        SubscribeLocalEvent<CentCommEyeOperatorComponent, MobStateChangedEvent>(OnOperatorMobStateChanged);
        SubscribeLocalEvent<CentCommEyeOperatorComponent, EntityTerminatingEvent>(OnOperatorTerminating);
        SubscribeLocalEvent<CentCommEyeOperatorComponent, MoveEvent>(OnOperatorMoved);
    }

    /// <summary>
    /// Handles in-world console interaction.
    /// </summary>
    private void OnActivate(Entity<CentCommEyeConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;

        if (ent.Comp.User != null)
        {
            _popup.PopupEntity(Loc.GetString("centcomm-eye-console-in-use"), ent, args.User);
            return;
        }

        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("centcomm-eye-console-unpowered"), ent, args.User);
            return;
        }

        if (!_access.IsAllowed(args.User, ent))
        {
            _popup.PopupEntity(Loc.GetString("centcomm-eye-console-access-denied"), ent, args.User);
            return;
        }

        if (!TryAttach(ent, args.User))
            _popup.PopupEntity(Loc.GetString("centcomm-eye-console-no-station"), ent, args.User);
    }

    /// <summary>
    /// Attaches the console user's mind to the eye.
    /// </summary>
    private bool TryAttach(Entity<CentCommEyeConsoleComponent> ent, EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mindId, out var mind) || mind.VisitingEntity != null)
            return false;

        if (ent.Comp.Eye is not { } eye || !Exists(eye))
        {
            if (GetObservationTarget(ent) is not { } coords)
                return false;

            eye = SpawnEye(ent, coords);
        }

        EnsureComp<CentCommEyeOperatorComponent>(user).Console = ent.Owner;

        ent.Comp.Eye = eye;
        ent.Comp.User = user;

        // PVS override to avoid entity pop-in on returning from the eye
        if (_player.TryGetSessionById(mind.UserId, out var session))
            _viewSubscriber.AddViewSubscriber(user, session);

        _mind.Visit(mindId, eye, mind);
        TransferFollowers(user, eye);

        return true;
    }

    /// <summary>
    /// Moves observer ghosts back and forth between the eye and the console operator.
    /// TODO: use system helper after upstream
    /// </summary>
    private void TransferFollowers(EntityUid from, EntityUid to)
    {
        if (!TryComp<FollowedComponent>(from, out var followed))
            return;

        foreach (var follower in followed.Following.ToArray())
        {
            _follower.StartFollowingEntity(follower, to);
        }
    }

    /// <summary>
    /// Set up the eye and it's actions.
    /// </summary>
    private EntityUid SpawnEye(Entity<CentCommEyeConsoleComponent> ent, EntityCoordinates coords)
    {
        var eye = SpawnAtPosition(ent.Comp.EyeProto, coords);

        var eyeComp = EnsureComp<CentCommEyeUserComponent>(eye);
        eyeComp.Console = ent.Owner;

        // The actions live on the eye, which is what the operator controls while observing.
        _actions.AddAction(eye, ref eyeComp.ReturnActionEntity, ReturnAction);
        _actions.AddAction(eye, ref eyeComp.ChannelsActionEntity, ChannelsAction);

        return eye;
    }

    /// <summary>
    /// Sends the operator back to their body. The eye remains on the station.
    /// </summary>
    private void Detach(Entity<CentCommEyeConsoleComponent> ent)
    {
        if (ent.Comp.User is not { } user)
            return;

        ent.Comp.User = null;

        if (!Exists(user))
            return;

        RemComp<CentCommEyeOperatorComponent>(user);

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return;

        if (_player.TryGetSessionById(mind.UserId, out var session))
            _viewSubscriber.RemoveViewSubscriber(user, session);

        if (ent.Comp.Eye is { } eyeUid && Exists(eyeUid))
            TransferFollowers(eyeUid, user);

        if (mind.VisitingEntity == ent.Comp.Eye)
            _mind.UnVisit(mindId, mind);
    }

    /// <summary>
    /// Deletes the eye. Fires when the observation console is removed.
    /// </summary>
    private void ClearEye(Entity<CentCommEyeConsoleComponent> ent)
    {
        Detach(ent);

        var eye = ent.Comp.Eye;
        ent.Comp.Eye = null;
        QueueDel(eye);
    }

    /// <summary>
    /// Finds a sensible spawn point for the eye on the target station.
    /// </summary>
    private EntityCoordinates? GetObservationTarget(Entity<CentCommEyeConsoleComponent> ent)
    {
        var consoleGrid = Transform(ent).GridUid;
        if (consoleGrid == null)
            return null;

        EntityUid? station = null;
        var stations = EntityQueryEnumerator<StationCentcommComponent>();
        while (stations.MoveNext(out var stationUid, out var centcomm))
        {
            if (centcomm.Entity != consoleGrid)
                continue;

            station = stationUid;
            break;
        }

        if (station == null)
            return null;

        if (_station.GetLargestGrid((station.Value, null)) is not { } grid)
            return null;

        var possible = new List<EntityCoordinates>();
        var spawns = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (spawns.MoveNext(out _, out var spawn, out var xform))
        {
            if (xform.GridUid != grid || spawn.SpawnType != SpawnPointType.LateJoin)
                continue;

            possible.Add(xform.Coordinates);
        }

        if (possible.Count > 0)
            return _random.Pick(possible);

        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return null;

        return new EntityCoordinates(grid, gridComp.LocalAABB.Center);
    }

    /// <summary>
    // Returns the console operator to their body.
    /// </summary>
    private void OnReturnAction(Entity<CentCommEyeUserComponent> ent, ref CentCommEyeReturnActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        DetachFromEye(ent);
    }

    private void OnEyeDetached(Entity<CentCommEyeUserComponent> ent, ref PlayerDetachedEvent args)
    {
        DetachFromEye(ent);
    }

    private void OnEyeTerminating(Entity<CentCommEyeUserComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!TryComp<CentCommEyeConsoleComponent>(ent.Comp.Console, out var console) || console.Eye != ent.Owner)
            return;

        Detach((ent.Comp.Console, console));
        console.Eye = null;
    }

    private void DetachFromEye(Entity<CentCommEyeUserComponent> ent)
    {
        if (TryComp<CentCommEyeConsoleComponent>(ent.Comp.Console, out var console) && console.Eye == ent.Owner)
        {
            Detach((ent.Comp.Console, console));
            return;
        }

        QueueDel(ent.Owner);
    }

    private void OnOperatorMobStateChanged(Entity<CentCommEyeOperatorComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        DetachFromOperator(ent);
    }

    private void OnOperatorTerminating(Entity<CentCommEyeOperatorComponent> ent, ref EntityTerminatingEvent args)
    {
        DetachFromOperator(ent);
    }

    private void OnOperatorMoved(Entity<CentCommEyeOperatorComponent> ent, ref MoveEvent args)
    {
        if (!TryComp<CentCommEyeConsoleComponent>(ent.Comp.Console, out var console))
        {
            DetachFromOperator(ent);
            return;
        }

        if (_xform.InRange(args.NewPosition, Transform(ent.Comp.Console).Coordinates, console.MaxUserRange))
            return;

        Detach((ent.Comp.Console, console));
    }

    private void DetachFromOperator(Entity<CentCommEyeOperatorComponent> ent)
    {
        if (TryComp<CentCommEyeConsoleComponent>(ent.Comp.Console, out var console) && console.User == ent.Owner)
        {
            Detach((ent.Comp.Console, console));
            return;
        }

        RemCompDeferred<CentCommEyeOperatorComponent>(ent.Owner);
    }

    private void OnPowerChanged(Entity<CentCommEyeConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        Detach(ent);
    }

    private void OnConsoleShutdown(Entity<CentCommEyeConsoleComponent> ent, ref ComponentShutdown args)
    {
        ClearEye(ent);
    }
}
