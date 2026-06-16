// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Forensics.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Paper;
using Content.Shared.SS220.Surgery.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Surgery.Systems;

public sealed class ImplantCheckInSurgerySystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    private static readonly LocId ImplantCheckNoImplantSlot = "implant-check-surgery-no-implants";
    private static readonly LocId ImplantCheckReportTitle = "implant-check-report-title";
    private static readonly LocId ImplantCheckHeader = "implant-check-report-header";
    private static readonly LocId ImplantCheckReportImplantEntry = "implant-check-report-implant-entry";

    public bool MakeImplantCheckPaper(EntityUid user, Entity<ImplantCheckInSurgeryComponent?> used, EntityUid target)
    {
        if (!Resolve(used.Owner, ref used.Comp))
            return false;

        if (!_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            _popup.PopupClient(ImplantCheckNoImplantSlot, user);
        }

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

        foreach (var implant in implants)
        {
            if (HasComp<HiddenInstalledImplantComponent>(implant))
                continue;

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
