// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Systems;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryStarterComponent, StartSurgeryMessage>(OnStartSurgeryMessage);
        SubscribeLocalEvent<SurgeryPatientComponent, MoveEvent>(OnSurgeryPatientMove);
    }

    private void OnStartSurgeryMessage(Entity<SurgeryStarterComponent> entity, ref StartSurgeryMessage args)
    {
        var user = GetEntity(args.User);
        var used = GetEntity(args.Used);
        var target = GetEntity(args.Target);

        if (user != args.Actor || used != entity.Owner || !Exists(target))
            return;

        if (!_interaction.InRangeUnobstructed(args.Actor, target))
            return;

        var ev = new StartSurgeryEvent(args.SurgeryGraphId, args.Target, args.User, args.Used);
        RaiseLocalEvent(entity, ev);
    }

    private void OnSurgeryPatientMove(Entity<SurgeryPatientComponent> entity, ref MoveEvent args)
    {
        var countForBleed = 0;
        foreach (var (key, nodeId) in entity.Comp.OngoingSurgeries)
        {
            if (!_prototypeManager.Resolve(key, out var resolvedSurgery))
                continue;

            if (nodeId == resolvedSurgery.Start)
                continue;

            countForBleed++;
        }

        if (countForBleed == 0)
            return;

        var distance = MathF.Min((args.NewPosition.Position - args.OldPosition.Position).Length(), 4f);

        _bloodstream.TryModifyBleedAmount(entity.Owner, entity.Comp.OnSurgeryMoveBleed * distance * countForBleed);
    }

    protected override void ProceedToNextStep(Entity<SurgeryPatientComponent> entity, EntityUid user, EntityUid? used, ProtoId<SurgeryGraphPrototype> surgeryGraph, SurgeryGraphEdge chosenEdge, bool predicted)
    {
        foreach (var action in SurgeryGraph.GetActions(chosenEdge))
        {
            action.PerformAction(entity.Owner, user, used, EntityManager);
        }

        // base comes after actions because it changes surgery node
        base.ProceedToNextStep(entity, user, used, surgeryGraph, chosenEdge, predicted);
    }

    protected override void OnPatientPain(EntityUid patient)
    {
        _chat.TryEmoteWithChat(patient, ScreamEmote, ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }
}
