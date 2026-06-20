// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.Hands.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Paper;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.Surgery.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Surgery.Systems;

public sealed class ImplantCheckInSurgerySystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private HandsSystem _handsSystem = default!;
    [Dependency] private ExperienceSystem _experience = default!;

    private static readonly LocId ImplantCheckReportTitle = "implant-check-report-title";
    private static readonly LocId ImplantCheckHeader = "implant-check-report-header";
    private static readonly LocId ImplantCheckReportImplantEntry = "implant-check-report-implant-entry";

    private static readonly ProtoId<SkillTreePrototype> AnatomySkillTree = "Anatomy";

    public bool MakeImplantCheckPaper(EntityUid user, Entity<ImplantCheckInSurgeryComponent?> used, EntityUid target)
    {
        if (!Resolve(used.Owner, ref used.Comp))
            return false;

        _container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer);
        var implantsList = implantContainer?.ContainedEntities ?? [];

        return MakePaper((used.Owner, used.Comp), user, target, implantsList);
    }

    private bool MakePaper(Entity<ImplantCheckInSurgeryComponent> entity, EntityUid user, EntityUid target, IReadOnlyList<EntityUid> implants)
    {
        var printed = Spawn(entity.Comp.OutputPaper, Transform(entity.Owner).Coordinates);
        _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed paper did not have PaperComponent.");
            return false;
        }

        string targetDNA = "err";
        if (TryComp<DnaComponent>(target, out var dnaComponent) && dnaComponent.DNA is not null)
        {
            targetDNA = dnaComponent.DNA;
        }

        _metaData.SetEntityName(printed, Loc.GetString(ImplantCheckReportTitle, ("dna", targetDNA)));

        var text = new StringBuilder();

        text.AppendLine(Loc.GetString(ImplantCheckHeader, ("dna", targetDNA)));

        var level = _experience.GetSkillTreeLevel(user, AnatomySkillTree) ?? (int)HiddenInstallLevel.Easy;
        foreach (var implant in implants)
        {
            if (TryComp<HiddenInstalledImplantComponent>(implant, out var installedImplantComponent))
            {
                if ((int)installedImplantComponent.InstallLevel > level)
                    continue;

                if (installedImplantComponent.Hidden)
                    continue;
            }

            string implantName;
            var protoId = MetaData(implant)?.EntityPrototype?.ID;
            if (protoId is not null)
            {
                var locData = Loc.GetEntityData(protoId);
                implantName = locData.Attributes.FirstOrNull(x => x.Key == "true-name")?.Value ??
                            string.Join(" ", MetaData(implant).EntityName, locData.Suffix);
            }
            else
            {
                implantName = MetaData(implant).EntityName;
            }

            text.AppendLine(Loc.GetString(ImplantCheckReportImplantEntry, ("implantName", implantName)));
        }

        _paper.SetContent((printed, paperComp), text.ToString());
        _audio.PlayPvs(entity.Comp.PrintSound, entity.Owner);

        return true;
    }
}
