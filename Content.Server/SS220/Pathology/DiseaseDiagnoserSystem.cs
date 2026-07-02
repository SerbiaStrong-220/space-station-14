// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.SS220.Photocopier.Forms;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class DiseaseDiagnoserSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private FormManager _formManager = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDiagnoserComponent, ComponentStartup>(OnUiDirty);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, BoundUIOpenedEvent>(OnUiDirty);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, DiseaseDiagnoserScanMessage>(OnScan);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, DiseaseDiagnoserTransferMutagenMessage>(OnTransferMutagen);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, DiseaseDiagnoserCopyMessage>(OnCopy);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, DiseaseDiagnoserPrintMessage>(OnPrint);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnUiDirty<T>(Entity<DiseaseDiagnoserComponent> ent, ref T args)
    {
        UpdateUi(ent);
    }

    private void OnSolutionChanged(Entity<DiseaseDiagnoserComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId == ent.Comp.BufferSolutionId)
            UpdateUi(ent);
    }

    private void OnEntInserted(Entity<DiseaseDiagnoserComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        OnSlotChanged(ent, args.Container.ID);
    }

    private void OnEntRemoved(Entity<DiseaseDiagnoserComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        OnSlotChanged(ent, args.Container.ID);
    }

    private void OnSlotChanged(Entity<DiseaseDiagnoserComponent> ent, string containerId)
    {
        if (containerId != ent.Comp.SlotId)
            return;

        ent.Comp.ScanEndTime = null;
        ent.Comp.PrintEndTime = null;
        ClearResult(ent);
        UpdateUi(ent);
    }

    private void OnScan(Entity<DiseaseDiagnoserComponent> ent, ref DiseaseDiagnoserScanMessage args)
    {
        // the UI stays open after power is cut, so gate the action itself, not just opening
        if (!this.IsPowered(ent, EntityManager))
            return;

        if (ent.Comp.ScanEndTime != null || ent.Comp.PrintEndTime != null)
            return;

        if (_itemSlots.GetItemOrNull(ent, ent.Comp.SlotId) == null)
            return;

        ClearResult(ent);
        ent.Comp.ScanEndTime = _timing.CurTime + ent.Comp.ScanDuration;
        UpdateUi(ent);
    }

    private void OnTransferMutagen(Entity<DiseaseDiagnoserComponent> ent, ref DiseaseDiagnoserTransferMutagenMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null
            || !_solutionContainer.TryGetFitsInDispenser(item.Value, out var sourceSoln, out var source)
            || !_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var bufferSoln, out var buffer))
            return;

        var available = source.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent);
        var amount = FixedPoint2.Min(available, buffer.MaxVolume - buffer.Volume);
        if (amount <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("disease-diagnoser-no-mutagen-source"), ent, args.Actor);
            return;
        }

        _solutionContainer.RemoveReagent(sourceSoln.Value, ent.Comp.MutagenReagent, amount);
        _solutionContainer.TryAddReagent(bufferSoln.Value, ent.Comp.MutagenReagent, amount, out _);
        // buffer change fires SolutionContainerChangedEvent -> OnSolutionChanged -> UpdateUi
    }

    private void OnCopy(Entity<DiseaseDiagnoserComponent> ent, ref DiseaseDiagnoserCopyMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null || !_solutionContainer.TryGetFitsInDispenser(item.Value, out _, out var source))
            return;

        var viruses = new List<VirusInstance>();
        foreach (var virus in VirusData.EnumerateViruses(source))
            viruses.Add(virus.Clone());
        if (viruses.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("disease-diagnoser-no-virus"), ent, args.Actor);
            return;
        }

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out var bufferSoln, out var buffer)
            || buffer.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent) < ent.Comp.CopyAmount)
        {
            _popup.PopupEntity(Loc.GetString("disease-diagnoser-not-enough-mutagen"), ent, args.Actor);
            return;
        }

        var bottle = Spawn(ent.Comp.CopyBottle, Transform(ent).Coordinates);
        if (!_solutionContainer.TryGetSolution(bottle, ent.Comp.BottleSolutionId, out var bottleSoln, out _))
        {
            // misconfigured CopyBottle prototype: bail before consuming mutagen or reporting success
            Log.Error($"CopyBottle {ent.Comp.CopyBottle} has no '{ent.Comp.BottleSolutionId}' solution");
            Del(bottle);
            return;
        }

        _solutionContainer.RemoveReagent(bufferSoln.Value, ent.Comp.MutagenReagent, ent.Comp.CopyAmount);

        var virusData = new VirusData { Viruses = viruses };
        _solutionContainer.TryAddReagent(bottleSoln.Value, ent.Comp.MutagenReagent, ent.Comp.CopyAmount, out _, data: new List<ReagentData> { virusData });

        _popup.PopupEntity(Loc.GetString("disease-diagnoser-copied"), ent, args.Actor);
        // buffer consumption fires SolutionContainerChangedEvent -> OnSolutionChanged -> UpdateUi
    }

    private void OnPrint(Entity<DiseaseDiagnoserComponent> ent, ref DiseaseDiagnoserPrintMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        // a report can only be printed off an analysed sample, and not while the machine is busy
        if (!ent.Comp.HasResult || ent.Comp.ScanEndTime != null || ent.Comp.PrintEndTime != null)
            return;

        ent.Comp.PrintEndTime = _timing.CurTime + ent.Comp.PrintDuration;
        _audio.PlayPvs(ent.Comp.PrintSound, ent);
        UpdateUi(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DiseaseDiagnoserComponent>();
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

    private void BuildResult(Entity<DiseaseDiagnoserComponent> ent)
    {
        ClearResult(ent);
        ent.Comp.HasResult = true;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null || !_solutionContainer.TryGetFitsInDispenser(item.Value, out _, out var solution))
            return;

        // each virus is reported as its own block, so a symptom shared by two strains shows in both
        foreach (var virus in VirusData.EnumerateViruses(solution))
        {
            var block = new DiseaseDiagnoserVirusResult();

            var allReadable = true;
            foreach (var symptomId in virus.Symptoms)
            {
                string? description = null;
                var readable = virus.RevealedSymptoms.Contains(symptomId)
                    && _pathology.TryGetSymptomDescription(symptomId, out description)
                    && description != null;

                // symptom reported only if revealed, otherwise stay unreadable
                if (readable)
                    block.Symptoms.Add(_pathology.FormatSymptom(symptomId, description!, virus, showAccelerant: true));
                else
                {
                    allReadable = false;
                    block.UnreadableCount++;
                }
            }

            // spread vectors and name are reported only if every symptom has been revealed
            if (allReadable)
            {
                block.Transmission = GetVectors(virus.Transmission);
                if (virus.Name is { } name)
                    block.Name = name;
            }

            ent.Comp.ResultViruses.Add(block);
        }
    }

    private static VirusTransmissionVector GetVectors(VirusTransmission? transmission)
    {
        var vectors = VirusTransmissionVector.None;
        if (transmission == null)
            return vectors;

        if (transmission.ContactChance > 0f)
            vectors |= VirusTransmissionVector.Contact;

        if (transmission.ProximityChance > 0f)
            vectors |= VirusTransmissionVector.Proximity;

        return vectors;
    }

    // fills configured paper form with scan result
    private void FinishPrint(Entity<DiseaseDiagnoserComponent> ent)
    {
        var form = _formManager.TryGetFormFromDescriptor(
            new FormDescriptor(ent.Comp.FormCollection, ent.Comp.FormGroup, ent.Comp.FormId));
        if (form == null)
        {
            Log.Error($"Diagnoser form {ent.Comp.FormCollection}/{ent.Comp.FormGroup}/{ent.Comp.FormId} not found");
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

            var vectors = new List<string>();
            if ((virus.Transmission & VirusTransmissionVector.Contact) != 0)
                vectors.Add(Loc.GetString("disease-diagnoser-vector-contact"));
            if ((virus.Transmission & VirusTransmissionVector.Proximity) != 0)
                vectors.Add(Loc.GetString("disease-diagnoser-vector-proximity"));
            var transmission = vectors.Count > 0 ? string.Join(", ", vectors) : "—";
            report.AppendLine(Loc.GetString("pathology-report-transmission", ("vectors", transmission)));
            report.AppendLine();
        }

        var content = form.Content.Replace("$REPORT$", report.ToString().TrimEnd());

        var paper = Spawn(form.PrototypeId, Transform(ent).Coordinates);
        if (TryComp<PaperComponent>(paper, out var paperComp))
            _paper.SetContent((paper, paperComp), content);
        _metaData.SetEntityName(paper, form.EntityName);
    }

    private void UpdateUi(Entity<DiseaseDiagnoserComponent> ent)
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

        var bufferMutagen = 0f;
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out _, out var buffer))
            bufferMutagen = (float)buffer.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent);

        string? stationName = null;
        if (_station.GetOwningStation(ent.Owner) is { } station)
            stationName = Name(station);

        var state = new DiseaseDiagnoserBoundUserInterfaceState(
            hasSample,
            scanning,
            printing,
            operationEnd,
            operationDuration,
            ent.Comp.HasResult,
            new List<DiseaseDiagnoserVirusResult>(ent.Comp.ResultViruses),
            bufferMutagen,
            stationName);

        _ui.SetUiState(ent.Owner, DiseaseDiagnoserUiKey.Key, state);

        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<DiseaseDiagnoserComponent> ent)
    {
        var fill = 0f;
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out _, out var buffer)
            && buffer.MaxVolume > FixedPoint2.Zero)
        {
            var amount = buffer.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent);
            fill = Math.Clamp((float)(amount / buffer.MaxVolume), 0f, 1f);
        }

        _appearance.SetData(ent, DiseaseDiagnoserVisuals.Running, ent.Comp.PrintEndTime != null);
        _appearance.SetData(ent, DiseaseDiagnoserVisuals.Vial, GetVialVisual(ent));
        _appearance.SetData(ent, DiseaseDiagnoserVisuals.Buffer, fill);
    }

    private DiseaseDiagnoserVial GetVialVisual(Entity<DiseaseDiagnoserComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, ent.Comp.SlotId) is not { } item)
            return DiseaseDiagnoserVial.None;

        if (!_solutionContainer.TryGetFitsInDispenser(item, out _, out var solution) || solution.Volume <= FixedPoint2.Zero)
            return DiseaseDiagnoserVial.Empty;

        if (solution.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent) > FixedPoint2.Zero
            || solution.GetTotalPrototypeQuantity(ent.Comp.UnstableMutagenReagent) > FixedPoint2.Zero)
            return DiseaseDiagnoserVial.Mutagen;

        return DiseaseDiagnoserVial.Blood;
    }

    private static void ClearResult(Entity<DiseaseDiagnoserComponent> ent)
    {
        ent.Comp.HasResult = false;
        ent.Comp.ResultViruses = new();
    }
}
