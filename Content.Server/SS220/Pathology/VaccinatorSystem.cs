// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.Pathology;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Text;
using Content.Shared.Paper;
using Content.Server.SS220.Photocopier.Forms;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Pathology;

public sealed partial class VaccinatorSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private FormManager _formManager = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VaccinatorComponent, ComponentStartup>(OnUiDirty);
        SubscribeLocalEvent<VaccinatorComponent, BoundUIOpenedEvent>(OnUiDirty);
        SubscribeLocalEvent<VaccinatorComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<VaccinatorComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<VaccinatorComponent, VaccinatorScanMessage>(OnScan);
        SubscribeLocalEvent<VaccinatorComponent, VaccinatorTransferMessage>(OnTransfer);
        SubscribeLocalEvent<VaccinatorComponent, VaccinatorCreateVaccineMessage>(OnCreateVaccine);
        SubscribeLocalEvent<VaccinatorComponent, VaccinatorPrintMessage>(OnPrint);
        SubscribeLocalEvent<VaccinatorComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnUiDirty<T>(Entity<VaccinatorComponent> ent, ref T args)
    {
        UpdateUi(ent);
    }

    // the buffer display tracks its actual reagent level, so refresh on any buffer change rather than
    // hanging the update off whichever action (transfer/create) happened to move the reagent
    private void OnSolutionChanged(Entity<VaccinatorComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId == ent.Comp.BufferSolutionId)
            UpdateUi(ent);
    }

    private void OnEntInserted(Entity<VaccinatorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        OnSlotChanged(ent, args.Container.ID);
    }

    private void OnEntRemoved(Entity<VaccinatorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        OnSlotChanged(ent, args.Container.ID);
    }

    private void OnSlotChanged(Entity<VaccinatorComponent> ent, string containerId)
    {
        if (containerId != ent.Comp.SlotId)
            return;

        ent.Comp.ScanEndTime = null;
        ent.Comp.PrintEndTime = null;
        ClearResult(ent);
        UpdateUi(ent);
    }

    private void OnScan(Entity<VaccinatorComponent> ent, ref VaccinatorScanMessage args)
    {
        // the UI stays open after power is cut, so gate the action itself, not just opening
        if (!this.IsPowered(ent, EntityManager))
            return;

        if (ent.Comp.ScanEndTime != null || ent.Comp.PrintEndTime != null || _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId) == null)
            return;

        ClearResult(ent);
        ent.Comp.ScanEndTime = _timing.CurTime + ent.Comp.ScanDuration;
        UpdateUi(ent);
    }

    private void OnTransfer(Entity<VaccinatorComponent> ent, ref VaccinatorTransferMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null
            || !_solutionContainer.TryGetFitsInDispenser(item.Value, out var sourceSoln, out var source)
            || !_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var bufferSoln, out var buffer))
            return;

        var available = source.GetTotalPrototypeQuantity(ent.Comp.TricordrazineReagent);
        var amount = FixedPoint2.Min(available, buffer.MaxVolume - buffer.Volume);
        if (amount <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("vaccinator-no-tricordrazine-source"), ent, args.Actor);
            return;
        }

        _solutionContainer.RemoveReagent(sourceSoln.Value, ent.Comp.TricordrazineReagent, amount);
        _solutionContainer.TryAddReagent(bufferSoln.Value, ent.Comp.TricordrazineReagent, amount, out _);
        // buffer change fires SolutionContainerChangedEvent -> OnSolutionChanged -> UpdateUi
    }

    private void OnCreateVaccine(Entity<VaccinatorComponent> ent, ref VaccinatorCreateVaccineMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null || !_solutionContainer.TryGetFitsInDispenser(item.Value, out _, out var source))
            return;
        var strains = new HashSet<string>();

        foreach (var virus in VirusData.EnumerateViruses(source))
        {
            if (virus.SuppressedUntil is { } until && _timing.CurTime < until)
                strains.Add(_pathology.GetIdentity(virus));
        }

        if (strains.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("vaccinator-no-cured-blood"), ent, args.Actor);
            return;
        }

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var bufferSoln, out var buffer)
            || buffer.GetTotalPrototypeQuantity(ent.Comp.TricordrazineReagent) < ent.Comp.VaccineAmount)
        {
            _popup.PopupEntity(Loc.GetString("vaccinator-no-tricordrazine"), ent, args.Actor);
            return;
        }

        var bottle = Spawn(ent.Comp.VaccineBottle, Transform(ent).Coordinates);
        if (!_solutionContainer.TryGetSolution(bottle, ent.Comp.BottleSolutionId, out var bottleSoln, out _))
        {
            Log.Error($"VaccineBottle {ent.Comp.VaccineBottle} has no '{ent.Comp.BottleSolutionId}' solution");
            Del(bottle);
            return;
        }

        _solutionContainer.RemoveReagent(bufferSoln.Value, ent.Comp.TricordrazineReagent, ent.Comp.VaccineAmount);

        var vaccineData = new VaccineData { Strains = new List<string>(strains) };
        _solutionContainer.TryAddReagent(bottleSoln.Value, ent.Comp.VaccineReagent, ent.Comp.VaccineAmount, out _, data: new List<ReagentData> { vaccineData });

        _popup.PopupEntity(Loc.GetString("vaccinator-vaccine-created"), ent, args.Actor);
    }

    private void OnPrint(Entity<VaccinatorComponent> ent, ref VaccinatorPrintMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        if (!ent.Comp.HasResult || ent.Comp.ScanEndTime != null || ent.Comp.PrintEndTime != null)
            return;

        ent.Comp.PrintEndTime = _timing.CurTime + ent.Comp.PrintDuration;
        _audio.PlayPvs(ent.Comp.PrintSound, ent);
        UpdateUi(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<VaccinatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);

            if (comp.ScanEndTime is { } scanEnd && _timing.CurTime >= scanEnd)
            {
                comp.ScanEndTime = null;
                BuildResult(ent);
                UpdateUi(ent);
                continue;
            }

            if (comp.PrintEndTime is { } printEnd && _timing.CurTime >= printEnd)
            {
                comp.PrintEndTime = null;
                FinishPrint(ent);
                UpdateUi(ent);
            }
        }
    }

    private void BuildResult(Entity<VaccinatorComponent> ent)
    {
        ClearResult(ent);
        ent.Comp.HasResult = true;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null || !_solutionContainer.TryGetFitsInDispenser(item.Value, out _, out var solution))
            return;

        // each virus is reported as its own block, so a symptom shared by two strains shows in both
        foreach (var virus in VirusData.EnumerateViruses(solution))
        {
            var block = new VaccinatorVirusResult();

            if (virus.SuppressedUntil is { } until && _timing.CurTime < until)
                block.Suppressed = true;

            var allReadable = true;
            foreach (var symptomId in virus.Symptoms)
            {
                string? description = null;
                var readable = virus.RevealedSymptoms.Contains(symptomId)
                    && _pathology.TryGetSymptomDescription(symptomId, out description)
                    && description != null;

                if (readable)
                    block.Symptoms.Add(_pathology.FormatSymptom(symptomId, description!, virus));
                else
                {
                    allReadable = false;
                    block.UnreadableCount++;
                }
            }

            if (allReadable && virus.Name is { } name)
                block.Name = name;

            // the cure reagents only show once every symptom of the strain has been revealed
            if (virus.Cure is { } cure)
            {
                if (allReadable)
                {
                    foreach (var reagent in cure.Reagents)
                    {
                        if (_prototype.TryIndex<ReagentPrototype>(reagent, out var reagentProto))
                            block.CureReagents.Add(reagentProto.LocalizedName);
                    }
                }
                else
                {
                    block.CureHidden = true;
                }
            }

            ent.Comp.ResultViruses.Add(block);
        }
    }

    private void FinishPrint(Entity<VaccinatorComponent> ent)
    {
        var form = _formManager.TryGetFormFromDescriptor(
            new FormDescriptor(ent.Comp.FormCollection, ent.Comp.FormGroup, ent.Comp.FormId));
        if (form == null)
        {
            Log.Error($"Vaccinator form {ent.Comp.FormCollection}/{ent.Comp.FormGroup}/{ent.Comp.FormId} not found");
            return;
        }

        var report = new StringBuilder();
        foreach (var virus in ent.Comp.ResultViruses)
        {
            report.AppendLine(Loc.GetString("pathology-report-pathogen",
                ("name", virus.Name ?? Loc.GetString("pathology-report-pathogen-unknown"))));
            report.AppendLine(Loc.GetString("pathology-report-symptoms"));
            if (virus.Symptoms.Count > 0)
            {
                foreach (var symptom in virus.Symptoms)
                    report.AppendLine($" · {symptom}");
            }
            else
                report.AppendLine(" · —");

            report.AppendLine(Loc.GetString("pathology-report-unreadable", ("count", virus.UnreadableCount)));

            var cure = virus.CureHidden
                ? Loc.GetString("pathology-report-cure-hidden")
                : virus.CureReagents.Count > 0
                    ? string.Join(", ", virus.CureReagents)
                    : "—";
            report.AppendLine(Loc.GetString("pathology-report-cure", ("cure", cure)));
            report.AppendLine();
        }

        var content = form.Content.Replace("$REPORT$", report.ToString().TrimEnd());

        var paper = Spawn(form.PrototypeId, Transform(ent).Coordinates);
        if (TryComp<PaperComponent>(paper, out var paperComp))
            _paper.SetContent((paper, paperComp), content);
        _metaData.SetEntityName(paper, form.EntityName);
    }

    private void UpdateUi(Entity<VaccinatorComponent> ent)
    {
        var hasSample = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId) != null;
        var scanning = ent.Comp.ScanEndTime != null;
        var printing = ent.Comp.PrintEndTime != null;
        var operationEnd = ent.Comp.ScanEndTime ?? ent.Comp.PrintEndTime;
        var operationDuration = scanning
            ? ent.Comp.ScanDuration
            : printing
                ? ent.Comp.PrintDuration
                : TimeSpan.Zero;

        var bufferTricordrazine = 0f;
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out _, out var buffer))
            bufferTricordrazine = (float)buffer.GetTotalPrototypeQuantity(ent.Comp.TricordrazineReagent);

        string? stationName = null;
        if (_station.GetOwningStation(ent.Owner) is { } station)
            stationName = Name(station);

        var state = new VaccinatorBoundUserInterfaceState(
            hasSample,
            scanning,
            printing,
            operationEnd,
            operationDuration,
            ent.Comp.HasResult,
            new List<VaccinatorVirusResult>(ent.Comp.ResultViruses),
            bufferTricordrazine,
            stationName);

        _ui.SetUiState(ent.Owner, VaccinatorUiKey.Key, state);

        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<VaccinatorComponent> ent)
    {
        var fill = 0f;
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out _, out var buffer)
            && buffer.MaxVolume > FixedPoint2.Zero)
        {
            var amount = buffer.GetTotalPrototypeQuantity(ent.Comp.TricordrazineReagent);
            fill = Math.Clamp((float)(amount / buffer.MaxVolume), 0f, 1f);
        }

        _appearance.SetData(ent, VaccinatorVisuals.Running, ent.Comp.PrintEndTime != null);
        _appearance.SetData(ent, VaccinatorVisuals.Vial, GetVialVisual(ent));
        _appearance.SetData(ent, VaccinatorVisuals.BufferFill, fill);
    }

    private VaccinatorVial GetVialVisual(Entity<VaccinatorComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, ent.Comp.SlotId) is not { } item)
            return VaccinatorVial.None;

        if (!_solutionContainer.TryGetFitsInDispenser(item, out _, out var solution) || solution.Volume <= FixedPoint2.Zero)
            return VaccinatorVial.Empty;

        if (solution.GetTotalPrototypeQuantity(ent.Comp.TricordrazineReagent) > FixedPoint2.Zero)
            return VaccinatorVial.Tricordrazine;

        return VaccinatorVial.Blood;
    }

    private static void ClearResult(Entity<VaccinatorComponent> ent)
    {
        ent.Comp.HasResult = false;
        ent.Comp.ResultViruses = new();
    }
}
