// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Buckle;
using Content.Shared.Examine;
using Robust.Shared.Network;

namespace Content.Server.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected readonly SurgeryGraphSystem SurgeryGraph = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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

        SubscribeLocalEvent<SurgeryDrapeComponent, AfterInteractEvent>(OnDrapeInteract);
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

        if (!_buckleSystem.IsBuckled(args.Target.Value))
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

    private void OnDrapeInteract(Entity<SurgeryDrapeComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (!IsValidTarget(args.Target.Value, out var reasonLocPath) || !IsValidPerformer(args.User))
        {
            _popup.PopupCursor(reasonLocPath != null ? Loc.GetString(reasonLocPath) : null);
            return;
        }
        //SS220_Surgery: here must open UI and from it you should get protoId of surgery

        args.Handled = TryStartSurgery(args.Target.Value, "MindSlaveFix", args.User, args.Used);
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
        bool isAbleToPerform = false;
        foreach (var edge in currentNode.Edges)
        {
            // id any edges exist make it true
            isAbleToPerform = true;
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
            _audio.PlayPredicted(used.Comp.UsingSound, entity.Owner, user,
                                        AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        return true;
    }
}
