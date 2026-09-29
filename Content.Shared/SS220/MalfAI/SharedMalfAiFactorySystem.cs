// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Body;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.MalfAI;

public sealed partial class SharedMalfAiFactorySystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private SharedAmbientSoundSystem _ambience = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private static readonly SoundSpecifier ConvertStartSound =
        new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");

    private static readonly TimeSpan DenyPopupCooldown = TimeSpan.FromSeconds(1.5);

    private static readonly LocId ConvertStart = "malfai-factory-convert-start";
    private static readonly LocId ExamineGrinding = "malfai-factory-examine-grinding";
    private static readonly LocId NoPower = "malfai-factory-no-power";
    private static readonly LocId Unauthorized = "malfai-factory-unauthorized";
    private static readonly LocId Busy = "malfai-factory-busy";
    private static readonly LocId NotBody = "malfai-factory-not-body";
    private static readonly LocId NotHumanoid = "malfai-factory-not-humanoid";
    private static readonly LocId NotDead = "malfai-factory-not-dead";
    private static readonly LocId TooFar = "malfai-factory-too-far";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MalfAiFactoryComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<MalfAiFactoryComponent, DragDropTargetEvent>(OnDrop);
        SubscribeLocalEvent<MalfAiFactoryComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<MalfAiFactoryComponent, MalfAiFactoryFeedDoAfterEvent>(OnFeedDoAfter);
        SubscribeLocalEvent<MalfAiFactoryComponent, DoAfterAttemptEvent<MalfAiFactoryFeedDoAfterEvent>>(OnFeedAttempt);
        SubscribeLocalEvent<MalfAiFactoryComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MalfAiFactoryComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ActiveMalfAiFactoryComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<ActiveMalfAiFactoryComponent, ComponentShutdown>(OnActiveShutdown);
    }

    private void OnPowerChanged(Entity<MalfAiFactoryComponent> ent, ref PowerChangedEvent args)
    {
        if (!_net.IsClient && HasComp<ActiveMalfAiFactoryComponent>(ent))
        {
            if (!args.Powered)
            {
                ent.Comp.PausedAt ??= _timing.CurTime;
            }
            else if (ent.Comp.PausedAt is { } pausedAt)
            {
                var pausedFor = _timing.CurTime - pausedAt;
                ent.Comp.ConversionDue += pausedFor;
                ent.Comp.FinishDue += pausedFor;
                ent.Comp.PausedAt = null;
            }
        }

        var active = HasComp<ActiveMalfAiFactoryComponent>(ent);
        if (active)
            _ambience.SetAmbience(ent, args.Powered);

        var status = !args.Powered
            ? MalfAiFactoryStatus.Off
            : active
                ? HasComp<FinishingMalfAiFactoryComponent>(ent)
                    ? MalfAiFactoryStatus.Finishing
                    : MalfAiFactoryStatus.Active
                : MalfAiFactoryStatus.Idle;

        _appearance.SetData(ent, MalfAiFactoryVisuals.Status, status);
    }

    private void OnCanDrop(Entity<MalfAiFactoryComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.CanDrop = DenyReason(ent, args.User, args.Dragged) == null;
    }

    private void OnDrop(Entity<MalfAiFactoryComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryBeginFeed(ent, args.Dragged, args.User);
    }

    private void OnAfterInteractUsing(Entity<MalfAiFactoryComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !args.CanReach)
            return;

        args.Handled = true;
        TryBeginFeed(ent, args.Used, args.User);
    }

    private void TryBeginFeed(Entity<MalfAiFactoryComponent> ent, EntityUid body, EntityUid user)
    {
        if (DenyReason(ent, user, body) is { } reason)
        {
            DenyPopup(ent, user, reason);
            return;
        }

        if (!TryComp<PhysicsComponent>(body, out var physics))
        {
            DenyPopup(ent, user, NotBody);
            return;
        }

        var delay = TimeSpan.FromSeconds(ent.Comp.InsertionDelayPerMass * physics.FixturesMass);
        if (delay <= TimeSpan.Zero)
        {
            if (_net.IsClient)
                return;

            StartGrind(ent, body, user);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay,
            new MalfAiFactoryFeedDoAfterEvent(), ent, target: ent.Owner, used: body)
        {
            NeedHand = true,
            DistanceThreshold = 1.5f,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        });
    }

    private void OnFeedAttempt(Entity<MalfAiFactoryComponent> ent,
        ref DoAfterAttemptEvent<MalfAiFactoryFeedDoAfterEvent> args)
    {
        if (args.Event.Used is not { } body || DenyReason(ent, args.Event.User, body) != null)
            args.Cancel();
    }

    private void OnFeedDoAfter(Entity<MalfAiFactoryComponent> ent, ref MalfAiFactoryFeedDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Used is not { } body)
            return;

        if (DenyReason(ent, args.User, body) is { } reason)
        {
            DenyPopup(ent, args.User, reason);
            return;
        }

        args.Handled = true;

        _audio.PlayPredicted(ConvertStartSound, ent, args.User);
        _popup.PopupClient(Loc.GetString(ConvertStart), ent, args.User);

        if (_net.IsClient)
            return;

        StartGrind(ent, body, args.User);
    }

    private void StartGrind(Entity<MalfAiFactoryComponent> ent, EntityUid body, EntityUid user)
    {
        var carried = new List<EntityUid>(_inventory.GetHandOrInventoryEntities(body));
        foreach (var item in carried)
            _xform.DropNextTo(item, ent.Owner);

        EntityUid? victimMind = null;
        if (_mind.TryGetMind(body, out var mindId, out var mind)
            && mind.OwnedEntity == body
            && mind.UserId != null
            && _player.TryGetSessionById(mind.UserId.Value, out _))
            victimMind = mindId;

        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(user):player} fed {ToPrettyString(body):target} into {ToPrettyString(ent):reclaimer} (Malf AI robot factory).");

        _xform.DetachEntity(body);
        ent.Comp.PendingBody = body;
        ent.Comp.VictimMind = victimMind;
        ent.Comp.PausedAt = null;
        ent.Comp.ConversionDue = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(0f, ent.Comp.ConversionTime));
        EnsureComp<ActiveMalfAiFactoryComponent>(ent);
        _appearance.SetData(ent, MalfAiFactoryVisuals.Status, MalfAiFactoryStatus.Active);
    }

    private void DenyPopup(Entity<MalfAiFactoryComponent> ent, EntityUid viewer, LocId locKey)
    {
        if (_timing.CurTime < ent.Comp.NextDenyPopup)
            return;

        ent.Comp.NextDenyPopup = _timing.CurTime + DenyPopupCooldown;
        _popup.PopupClient(Loc.GetString(locKey), ent, viewer);
    }

    private void OnExamined(Entity<MalfAiFactoryComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ActiveMalfAiFactoryComponent>(ent))
            return;

        args.PushMarkup(Loc.GetString(_power.IsPowered(ent.Owner)
            ? ExamineGrinding
            : NoPower));
    }

    private void OnActiveInit(Entity<ActiveMalfAiFactoryComponent> ent, ref ComponentInit args)
    {
        if (_timing.ApplyingState)
            return;

        _ambience.SetAmbience(ent, true);
    }

    private void OnActiveShutdown(Entity<ActiveMalfAiFactoryComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        _ambience.SetAmbience(ent, false);
    }

    private LocId? DenyReason(Entity<MalfAiFactoryComponent> ent, EntityUid user, EntityUid dragged)
    {
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(user) || TerminatingOrDeleted(dragged))
            return Unauthorized;

        if (!HasComp<HandsComponent>(user)
            || !_actionBlocker.CanInteract(user, ent.Owner)
            || !_actionBlocker.CanInteract(user, dragged))
            return Unauthorized;

        if (HasComp<ActiveMalfAiFactoryComponent>(ent))
            return Busy;

        if (!_power.IsPowered(ent.Owner))
            return NoPower;

        if (!HasComp<BodyComponent>(dragged) || !HasComp<MobStateComponent>(dragged))
            return NotBody;

        if (!HasComp<HumanoidProfileComponent>(dragged))
            return NotHumanoid;

        if (!_mobState.IsDead(dragged))
            return NotDead;

        if (!_interaction.InRangeAndAccessible(ent.Owner, dragged)
            || !_interaction.InRangeUnobstructed(user, ent.Owner)
            || !_interaction.InRangeUnobstructed(user, dragged))
            return TooFar;

        return null;
    }
}
