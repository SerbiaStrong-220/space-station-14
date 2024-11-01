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

namespace Content.Server.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OnSurgeryComponent, InteractUsingEvent>(OnSurgeryInteractUsing);
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

    private void OnDrapeInteract(Entity<SurgeryDrapeComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !IsValidTarget(args.Target.Value) || !IsValidPerformer(args.User))
            return;

        //SS220_Surgery: here must open UI and from it you should get protoId of surgery

        args.Handled = TryStartSurgery(args.Target.Value, "MindSlaveFix");
    }

    public bool TryStartSurgery(EntityUid target, ProtoId<SurgeryGraphPrototype> surgery)
    {
        if (HasComp<OnSurgeryComponent>(target))
        {
            Log.Error("Patient which is already on surgery is tried for surgery again");
            return false;
        }

        var onSurgery = AddComp<OnSurgeryComponent>(target);
        onSurgery.SurgeryGraphProtoId = surgery;
        StartSurgeryNode((target, onSurgery));

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
        // allocate here
        SurgeryGraphEdge? chosenEdge = null;
        bool isAbleToPerform = false;
        foreach (var edge in entity.Comp.CurrentNode.Edges)
        {
            // id any edges exist make it true
            isAbleToPerform = true;
            foreach (var condition in edge.Conditions)
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

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(chosenEdge.Delay), new SurgeryDoAfterEvent(chosenEdge),
                            entity.Owner, target: entity.Owner, used: used.Owner)
            {
                NeedHand = true,
                BreakOnMove = true,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);

        if (used.Comp.StartSurgerySound != null)
            _audio.PlayPvs(used.Comp.StartSurgerySound, entity.Owner,
                            AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        return true;
    }
}
