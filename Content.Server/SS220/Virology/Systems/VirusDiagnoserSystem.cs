// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.SS220.Photocopier.Forms;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;
using Content.Shared.SS220.Virology;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology;

public sealed partial class VirusDiagnoserSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private VirologySystem _virology = default!;
    [Dependency] private FormManager _formManager = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusDiagnoserComponent, ComponentStartup>(OnUiDirty);
        SubscribeLocalEvent<VirusDiagnoserComponent, BoundUIOpenedEvent>(OnUiDirty);
        SubscribeLocalEvent<VirusDiagnoserComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<VirusDiagnoserComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<VirusDiagnoserComponent, VirusDiagnoserScanMessage>(OnScan);
        SubscribeLocalEvent<VirusDiagnoserComponent, VirusDiagnoserTransferMutagenMessage>(OnTransferMutagen);
        SubscribeLocalEvent<VirusDiagnoserComponent, VirusDiagnoserCopyMessage>(OnCopy);
        SubscribeLocalEvent<VirusDiagnoserComponent, VirusDiagnoserPrintMessage>(OnPrint);
        SubscribeLocalEvent<VirusDiagnoserComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnUiDirty<T>(Entity<VirusDiagnoserComponent> ent, ref T args)
    {
        UpdateUi(ent);
    }

    private void OnSolutionChanged(Entity<VirusDiagnoserComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId == ent.Comp.BufferSolutionId)
            UpdateUi(ent);
    }

    private void OnEntInserted(Entity<VirusDiagnoserComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        OnSlotChanged(ent, args.Container.ID);
    }

    private void OnEntRemoved(Entity<VirusDiagnoserComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        OnSlotChanged(ent, args.Container.ID);
    }

    private void OnSlotChanged(Entity<VirusDiagnoserComponent> ent, string containerId)
    {
        if (containerId != ent.Comp.SlotId)
            return;

        if (ent.Comp.PrintEndTime != null)
            return;

        ent.Comp.ScanEndTime = null;
        ClearResult(ent);
        UpdateUi(ent);
    }

    private void OnScan(Entity<VirusDiagnoserComponent> ent, ref VirusDiagnoserScanMessage args)
    {
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

    private void OnTransferMutagen(Entity<VirusDiagnoserComponent> ent, ref VirusDiagnoserTransferMutagenMessage args)
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

        source.RemoveReagent(new ReagentId(ent.Comp.MutagenReagent, null), amount, ignoreReagentData: true);
        _solutionContainer.UpdateChemicals(sourceSoln.Value);
        _solutionContainer.TryAddReagent(bufferSoln.Value, ent.Comp.MutagenReagent, amount, out _);
    }

    private void OnCopy(Entity<VirusDiagnoserComponent> ent, ref VirusDiagnoserCopyMessage args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null || !_solutionContainer.TryGetFitsInDispenser(item.Value, out _, out var source))
            return;

        var descriptors = new List<VirusDescriptor>();
        foreach (var virus in VirusData.EnumerateViruses(source))
            descriptors.Add(virus.Clone());

        if (descriptors.Count == 0)
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
            Log.Error($"CopyBottle {ent.Comp.CopyBottle} has no '{ent.Comp.BottleSolutionId}' solution");
            Del(bottle);
            return;
        }

        _solutionContainer.RemoveReagent(bufferSoln.Value, ent.Comp.MutagenReagent, ent.Comp.CopyAmount);

        var virusData = new VirusData { Viruses = descriptors };
        _solutionContainer.TryAddReagent(bottleSoln.Value, ent.Comp.MutagenReagent, ent.Comp.CopyAmount, out _, data: [virusData]);

        _popup.PopupEntity(Loc.GetString("disease-diagnoser-copied"), ent, args.Actor);
    }

    private void OnPrint(Entity<VirusDiagnoserComponent> ent, ref VirusDiagnoserPrintMessage args)
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
        var query = EntityQueryEnumerator<VirusDiagnoserComponent>();
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

    private void BuildResult(Entity<VirusDiagnoserComponent> ent)
    {
        ClearResult(ent);
        ent.Comp.HasResult = true;

        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.SlotId);
        if (item == null || !_solutionContainer.TryGetFitsInDispenser(item.Value, out _, out var solution))
            return;

        foreach (var virus in VirusData.EnumerateViruses(solution))
        {
            var block = new VirusDiagnoserResult();

            var allReadable = true;
            foreach (var snapshot in virus.Symptoms)
            {
                string? description = null;
                var readable = snapshot.Revealed
                    && _virology.TryGetSymptomDescription(snapshot.Symptom, out description)
                    && description != null;

                if (readable)
                    block.Symptoms.Add(_virology.FormatSymptom(snapshot.Symptom, description!, snapshot.Stage, snapshot.Accelerant));
                else
                {
                    allReadable = false;
                    block.UnreadableCount++;
                }
            }

            if (allReadable)
            {
                block.Transmission = GetVectors(_virology.ResolveTransmission(virus));
                if (_virology.ResolveName(virus) is { } name)
                    block.Name = name;
            }

            ent.Comp.ScanResults.Add(block);
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

    private void FinishPrint(Entity<VirusDiagnoserComponent> ent)
    {
        var form = _formManager.TryGetFormFromDescriptor(
            new FormDescriptor(ent.Comp.FormCollection, ent.Comp.FormGroup, ent.Comp.FormId));
        if (form == null)
        {
            Log.Error($"Diagnoser form {ent.Comp.FormCollection}/{ent.Comp.FormGroup}/{ent.Comp.FormId} not found");
            return;
        }

        var report = new StringBuilder();
        foreach (var virus in ent.Comp.ScanResults)
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

    private void UpdateUi(Entity<VirusDiagnoserComponent> ent)
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

        var status = !hasSample ? VirologyMachineStatus.NoSample
            : scanning ? VirologyMachineStatus.Scanning
            : printing ? VirologyMachineStatus.Printing
            : ent.Comp.HasResult ? VirologyMachineStatus.Result
            : VirologyMachineStatus.Ready;

        var bufferMutagen = 0f;
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out _, out var buffer))
            bufferMutagen = (float)buffer.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent);

        string? stationName = null;
        if (_station.GetOwningStation(ent.Owner) is { } station)
            stationName = Name(station);

        var state = new VirusDiagnoserBoundUserInterfaceState(
            status,
            operationEnd,
            operationDuration,
            [.. ent.Comp.ScanResults],
            bufferMutagen,
            stationName);

        _ui.SetUiState(ent.Owner, VirusDiagnoserUiKey.Key, state);

        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<VirusDiagnoserComponent> ent)
    {
        var fill = 0f;
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.BufferSolutionId, out _, out var buffer)
            && buffer.MaxVolume > FixedPoint2.Zero)
        {
            var amount = buffer.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent);
            fill = Math.Clamp((float)(amount / buffer.MaxVolume), 0f, 1f);
        }

        _appearance.SetData(ent, VirusDiagnoserVisuals.Running, ent.Comp.PrintEndTime != null);
        _appearance.SetData(ent, VirusDiagnoserVisuals.Vial, GetVialVisual(ent));
        _appearance.SetData(ent, VirusDiagnoserVisuals.Buffer, fill);
    }

    private VirusDiagnoserVial GetVialVisual(Entity<VirusDiagnoserComponent> ent)
    {
        if (_itemSlots.GetItemOrNull(ent, ent.Comp.SlotId) is not { } item)
            return VirusDiagnoserVial.None;

        if (!_solutionContainer.TryGetFitsInDispenser(item, out _, out var solution) || solution.Volume <= FixedPoint2.Zero)
            return VirusDiagnoserVial.Empty;

        if (solution.GetTotalPrototypeQuantity(ent.Comp.MutagenReagent) > FixedPoint2.Zero
            || solution.GetTotalPrototypeQuantity(ent.Comp.UnstableMutagenReagent) > FixedPoint2.Zero)
            return VirusDiagnoserVial.Mutagen;

        return VirusDiagnoserVial.Blood;
    }

    private static void ClearResult(Entity<VirusDiagnoserComponent> ent)
    {
        ent.Comp.HasResult = false;
        ent.Comp.ScanResults = [];
    }
}
