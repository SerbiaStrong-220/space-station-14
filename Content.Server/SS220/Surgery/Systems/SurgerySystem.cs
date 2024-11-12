// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Audio;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.SS220.Surgery.Systems;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OnSurgeryComponent, SurgeryDoAfterEvent>(OnSurgeryDoAfter);
    }

    private void OnSurgeryDoAfter(Entity<OnSurgeryComponent> entity, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ProceedToNextStep(entity, args.User, args.Used, args.TargetEdge);
    }

    private void ProceedToNextStep(Entity<OnSurgeryComponent> entity, EntityUid user, EntityUid? used, SurgeryGraphEdge chosenEdge)
    {
        foreach (var action in SurgeryGraph.GetActions(chosenEdge))
        {
            action.PerformAction(entity.Owner, user, used, EntityManager);
        }

        ChangeSurgeryNode(entity, chosenEdge.Target, user, used);

        _audio.PlayPvs(SurgeryGraph.GetSoundSpecifier(chosenEdge), entity.Owner,
                        AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        if (OperationEnded(entity))
            RemComp<OnSurgeryComponent>(entity.Owner);

        Dirty(entity);
    }
}
