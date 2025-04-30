// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Buckle;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.Examine;
using Robust.Shared.Network;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;


namespace Content.Server.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected readonly SurgeryGraphSystem SurgeryGraph = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private const float ErrorGettingDelayDelay = 8f;
    private const float DoAfterMovementThreshold = 0.15f;
    private const int SurgeryExaminePushPriority = -1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OnSurgeryComponent, InteractUsingEvent>(OnSurgeryInteractUsing);
        SubscribeLocalEvent<OnSurgeryComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<OnSurgeryComponent, DoAfterAttemptEvent<SurgeryDoAfterEvent>>((uid, comp, ev) =>
        {
            BuckleDoAfterEarly((uid, comp), ev.Event, ev);
        });
        SubscribeLocalEvent<OnSurgeryComponent, SurgeryDoAfterEvent>(OnSurgeryDoAfter);

        SubscribeLocalEvent<SurgeryStarterComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SurgeryStarterComponent, StartSurgeryEvent>(OnStartSurgeryMessage);
    }

    /// <summary>
    /// Yes, for now surgery is forced to have something done with surgeryTool
    /// </summary>
    private void OnSurgeryInteractUsing(Entity<OnSurgeryComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<SurgeryToolComponent>(args.Used, out var surgeryTool))
            return;

        args.Handled = TryPerformOperationStep(entity, (args.Used, surgeryTool), args.User);
    }

    private void OnExamined(Entity<OnSurgeryComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.CurrentNode == null)
            return;

        var graphProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        if (!graphProto.TryGetNode(entity.Comp.CurrentNode, out var currentNode))
            return;

        if (entity.Comp.CurrentNode != null
            && SurgeryGraph.ExamineDescription(currentNode) != null)
            args.PushMarkup(Loc.GetString(SurgeryGraph.ExamineDescription(currentNode)!), SurgeryExaminePushPriority);
    }

    private void BuckleDoAfterEarly(Entity<OnSurgeryComponent> entity, SurgeryDoAfterEvent args, CancellableEntityEventArgs ev)
    {
        if (args.Target == null || args.Used == null)
            return;

        if (!_buckle.IsBuckled(args.Target.Value))
            ev.Cancel();
    }

    private void OnSurgeryDoAfter(Entity<OnSurgeryComponent> entity, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled || entity.Comp.CurrentNode == null)
            return;

        var operationProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        if (!operationProto.TryGetNode(entity.Comp.CurrentNode, out var node))
            return;

        SurgeryGraphEdge? targetEdge = null;
        foreach (var edge in node.Edges)
        {
            if (edge.Target == args.TargetEdge)
            {
                targetEdge = edge;
                break;
            }
        }

        if (targetEdge == null)
        {
            if (_netManager.IsServer)
            {
                Log.Error("Got wrong target edge in surgery do after!");
            }
            return;
        }

        ProceedToNextStep(entity, args.User, args.Used, targetEdge);
    }

    private void OnAfterInteract(Entity<SurgeryStarterComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target))
            return;

        if (!_userInterface.HasUi(entity, SurgeryDrapeUiKey.Key))
        {
            Log.Debug($"Entity {ToPrettyString(entity)} has SurgeryStartComponent but don't have its UI!");
            return;
        }

        if (!TryComp<OnSurgeryComponent>(args.Target, out var onSurgeryComponent))
        {
            if (!_userInterface.IsUiOpen(entity.Owner, SurgeryDrapeUiKey.Key))
                _userInterface.OpenUi(entity.Owner, SurgeryDrapeUiKey.Key, predicted: true);

            UpdateUserInterface(entity, args.User, args.Target.Value);
            return;
        }

        if (OperationCanBeEnded(args.Target.Value))
        {
            _adminLogManager.Add(Shared.Database.LogType.Action, Shared.Database.LogImpact.Medium,
                $"{ToPrettyString(args.User):user}  stopped surgery (surgery_graph_id: {onSurgeryComponent.SurgeryGraphProtoId}) on {ToPrettyString(args.Target):target}");

            _popup.PopupPredicted(Loc.GetString("surgery-cancelled", ("target", args.Target), ("user", args.User)), args.Target.Value, args.User);
            EndOperation(args.Target.Value);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("surgery-cant-be-cancelled"));
        }
    }

    public void UpdateUserInterface(EntityUid drape, EntityUid user, EntityUid target)
    {
        var netUser = GetNetEntity(user);
        var netTarget = GetNetEntity(target);

        var state = new SurgeryDrapeUpdate(netUser, netTarget);
        _userInterface.SetUiState(drape, SurgeryDrapeUiKey.Key, state);
    }

    private void OnStartSurgeryMessage(Entity<SurgeryStarterComponent> entity, ref StartSurgeryEvent args)
    {
        var target = GetEntity(args.Target);
        var user = GetEntity(args.User);

        // TODO: make opening on yourself unavailable by drapes
        if (target == user)
            return;

        if (!IsValidTarget(target, args.SurgeryGraphId, out var reasonLocPath) || !IsValidPerformer(user, args.SurgeryGraphId))
        {
            // TODO more user friendly
            _popup.PopupClient(reasonLocPath != null ? Loc.GetString(reasonLocPath) : null, user, PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        var result = TryStartSurgery(target, args.SurgeryGraphId, user, entity) ? "success" : "unsuccess";

        _adminLogManager.Add(Shared.Database.LogType.Action, Shared.Database.LogImpact.Medium,
            $"{ToPrettyString(args.User):user} tried to start surgery(surgery_graph_id: {args.SurgeryGraphId}) on {ToPrettyString(args.Target):target} with result of {result}");
    }

    public bool TryStartSurgery(EntityUid target, ProtoId<SurgeryGraphPrototype> surgery, EntityUid performer, EntityUid used)
    {
        if (HasComp<OnSurgeryComponent>(target))
        {
            Log.Error("Patient which is already on surgery is tried for surgery again");
            return false;
        }

        var onSurgery = AddComp<OnSurgeryComponent>(target);
        onSurgery.SurgeryGraphProtoId = surgery;

        StartSurgeryNode((target, onSurgery), performer, used);

        return true;
    }

    /// <returns>true if operation step performed successful</returns>
    public bool TryPerformOperationStep(Entity<OnSurgeryComponent> entity, Entity<SurgeryToolComponent> used, EntityUid user)
    {
        if (entity.Comp.CurrentNode == null)
        {
            Log.Fatal("Tried to perform operation with null node or surgery graph proto");
            return false;
        }

        var graphProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        if (!graphProto.TryGetNode(entity.Comp.CurrentNode, out var currentNode))
        {
            Log.Fatal($"Current node of {ToPrettyString(entity)} has incorrect value {entity.Comp.CurrentNode} for graph proto {entity.Comp.SurgeryGraphProtoId}");
            return false;
        }

        SurgeryGraphEdge? chosenEdge = null;
        foreach (var edge in currentNode.Edges)
        {
            // id any edges exist make it true
            bool isAbleToPerform = true;
            foreach (var condition in SurgeryGraph.GetConditions(edge))
            {
                if (!condition.Condition(used, EntityManager))
                    isAbleToPerform = false;
            }
            // if passed all conditions than break
            if (isAbleToPerform)
            {
                chosenEdge = edge;
                break;
            }
        }
        // yep.. another check
        if (chosenEdge == null)
            return false;

        // lets be honest, I don't believe that everyone will check their's surgeryGraphPrototype mapping
        var delay = SurgeryGraph.Delay(chosenEdge);
        if (delay == null)
        {
            Log.Fatal($"Found edge with zero delay, graph id: {entity.Comp.SurgeryGraphProtoId}");
            delay = ErrorGettingDelayDelay;
        }

        var performerDoAfterEventArgs =
            new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(delay.Value),
                            new SurgeryDoAfterEvent(chosenEdge.Target), entity.Owner, target: entity.Owner, used: used.Owner)
            {
                NeedHand = true,
                BreakOnMove = true,
                MovementThreshold = DoAfterMovementThreshold,
                AttemptFrequency = AttemptFrequency.EveryTick
            };

        if (_doAfter.TryStartDoAfter(performerDoAfterEventArgs))
            _audio.PlayPredicted(used.Comp.UsingSound, entity.Owner, user, audioParams: used.Comp.UsingSound?.Params.WithVolume(1f));

        return true;
    }
}
