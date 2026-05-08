// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Body;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed partial class CultYoggSystem : SharedCultYoggSystem
{
    [Dependency] private readonly VisualBodySystem _visualBody = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<MarkingPrototype> CultDefaultMarking = "CultStage-Halo";
    private const string CultMarkingCommonPart = "CultStage";

    private static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    private static readonly ProtoId<OrganCategoryPrototype> TorsoCategory = "Torso"; // Special Layer exists only on torso

    private void EnsureEyesColor(Entity<CultYoggComponent> ent)
    {
        _visualBody.TryGatherMarkingsData(ent.Owner, [HumanoidVisualLayers.Eyes], out var eyesProfiles, out _, out _);

        if (eyesProfiles is null || !eyesProfiles.TryGetValue(EyesCategory, out var eyesProfile))
            return;

        if (ent.Comp.PreviousEyeColor is not null)
            return;

        ent.Comp.PreviousEyeColor = eyesProfile.EyeColor;
        eyesProfile.EyeColor = Color.Green;
        _visualBody.ApplyProfile(ent, eyesProfile);
    }

    private void EnsureHalo(Entity<CultYoggComponent> ent)
    {
        _visualBody.TryGatherMarkingsData(ent.Owner, [HumanoidVisualLayers.Special], out _, out _, out var appliedMarkings);
        if (appliedMarkings is null || !appliedMarkings.TryGetValue(TorsoCategory, out var torsoSpecialMarkings))
            return;

        // This is strange cause before I expicitly ask for organs with that layer
        if (!torsoSpecialMarkings.TryGetValue(HumanoidVisualLayers.Special, out var specialMarkingsList))
        {
            specialMarkingsList = new();
            torsoSpecialMarkings.Add(HumanoidVisualLayers.Special, specialMarkingsList);
        }

        if (specialMarkingsList.Any(x => x.MarkingId.Id.Contains(CultDefaultMarking)))
            return;

        var markingId = CultDefaultMarking;
        if (TryComp<HumanoidProfileComponent>(ent, out var humanoidProfile))
            markingId = $"{CultMarkingCommonPart}-{humanoidProfile.Species}";

        if (!_prototype.HasIndex<MarkingPrototype>(markingId))
            markingId = CultDefaultMarking;

        var haloMarking = new Marking(markingId, [Color.White]);
        specialMarkingsList.Add(haloMarking);

        _visualBody.ApplyMarkings(ent, new() { { TorsoCategory, torsoSpecialMarkings } });

        _visualBody.TryGatherMarkingsData(ent.Owner, [HumanoidVisualLayers.Tail], out _, out _, out var appliedTailMarkings);
        if (appliedTailMarkings is null || !appliedTailMarkings.TryGetValue(TorsoCategory, out var torsoTailMarkings))
            return;

        if (torsoTailMarkings.TryGetValue(HumanoidVisualLayers.Tail, out var tailMarkings))
        {
            ent.Comp.PreviousTailMarkings = tailMarkings.ShallowClone();

            // I am NOT PROUD OF IT but uh.... At the end of the day it works!
            tailMarkings = tailMarkings.Select(x => new Marking(x.MarkingId, x.MarkingColors.Select(x => x.WithAlpha(0.0f)))).ToList();

            _visualBody.ApplyMarkings(ent, new() { { TorsoCategory, new() { { HumanoidVisualLayers.Tail, tailMarkings } } } });
        }

    }
}
