// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Audio;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Linguini.Bundle.Errors;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.SS220.Surgery.Systems;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected override void ProceedToNextStep(Entity<OnSurgeryComponent> entity, EntityUid user, EntityUid? used, SurgeryGraphEdge chosenEdge)
    {
        foreach (var action in SurgeryGraph.GetActions(chosenEdge))
        {
            action.PerformAction(entity.Owner, user, used, EntityManager);
        }

        base.ProceedToNextStep(entity, user, used, chosenEdge);

        Dirty(entity);
    }


}
