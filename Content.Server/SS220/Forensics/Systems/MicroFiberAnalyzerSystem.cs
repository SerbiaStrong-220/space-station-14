// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Server.SS220.Photocopier;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.SS220.Forensics;
using Content.Shared.SS220.Photocopier;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Analyzes physical glove-trace samples and retains private snapshots for later viewing and printing.
/// </summary>
public sealed class MicroFiberAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly PhotocopierSystem _photocopier = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly TimeSpan PrinterCheckInterval = TimeSpan.FromMilliseconds(250);
    private TimeSpan _nextPrinterCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MicroFiberAnalyzerRequestResultsMessage>(
            OnRequestResults);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MicroFiberAnalyzerEjectMessage>(OnEject);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MicroFiberAnalyzerAnalyzeMessage>(OnAnalyze);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MicroFiberAnalyzerSetDescriptionMessage>(
            OnSetDescription);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MicroFiberAnalyzerPrintMessage>(OnPrint);
        SubscribeLocalEvent<MicroFiberAnalyzerComponent, MicroFiberAnalyzerPrintComparisonMessage>(
            OnPrintComparison);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var checkPrinter = _timing.CurTime >= _nextPrinterCheck;
        if (checkPrinter)
            _nextPrinterCheck = _timing.CurTime + PrinterCheckInterval;

        var query = EntityQueryEnumerator<MicroFiberAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            Entity<MicroFiberAnalyzerComponent> ent = (uid, component);
            if (checkPrinter)
                UpdatePrinterStatus(ent);

            UpdateAnalysis(ent);
        }
    }

    private void UpdateAnalysis(Entity<MicroFiberAnalyzerComponent> ent)
    {
        if (ent.Comp.AnalyzingSwab == null)
            return;

        if (!TryGetSample(ent, out var sample))
        {
            CancelPendingAnalysis(ent);
            return;
        }

        if (sample.Owner != ent.Comp.AnalyzingSwab.Value || !sample.Comp.Used)
        {
            CancelPendingAnalysis(ent);
            return;
        }

        if (_timing.CurTime < ent.Comp.AnalysisFinishAt)
            return;

        CompleteAnalysis(ent, sample);
    }

    private void CompleteAnalysis(
        Entity<MicroFiberAnalyzerComponent> ent,
        Entity<GloveMicroFiberSwabComponent> sample)
    {
        var id = ent.Comp.History.Count + 1;
        var identifier = $"{_gameTicker.RoundId}-{ent.Owner.Id}-{id}";
        ent.Comp.History.Add(new MicroFiberAnalyzerHistoryEntry(id, [.. sample.Comp.GloveMicroFibers],
            description: ent.Comp.PendingDescription,
            identifier: identifier,
            sourceName: sample.Comp.SourceName,
            storageMicroFibers: [.. sample.Comp.StorageMicroFibers],
            analyzedAt: _gameTicker.RoundDuration()));
        ResetAnalysis(ent);
        ent.Comp.PendingDescription = string.Empty;
        Del(sample.Owner);
        SendResults(ent);
    }

    private void CancelPendingAnalysis(Entity<MicroFiberAnalyzerComponent> ent)
    {
        CancelAnalysis(ent);
        ent.Comp.PendingDescription = string.Empty;
        SendResults(ent);
    }

    private void OnMapInit(Entity<MicroFiberAnalyzerComponent> ent, ref MapInitEvent args)
    {
        UpdatePrinterStatus(ent);
    }

    private void OnLinkAttempt(Entity<MicroFiberAnalyzerComponent> ent, ref LinkAttemptEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != MicroFiberAnalyzerUi.ReportSourcePort.Id)
            return;

        if (args.SinkPort != MicroFiberAnalyzerUi.ReportSinkPort.Id)
        {
            args.Cancel();
            return;
        }

        if (!HasComp<PhotocopierComponent>(args.Sink))
            args.Cancel();
    }

    private void OnNewLink(Entity<MicroFiberAnalyzerComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != MicroFiberAnalyzerUi.ReportSourcePort.Id)
            return;

        if (args.SinkPort != MicroFiberAnalyzerUi.ReportSinkPort.Id)
            return;

        if (!HasComp<PhotocopierComponent>(args.Sink))
            return;

        foreach (var previous in _deviceLink.GetLinkedSinks((ent.Owner, null),
            MicroFiberAnalyzerUi.ReportSourcePort))
        {
            if (previous == args.Sink)
                continue;

            _deviceLink.RemoveSinkFromSource(ent, previous);
        }

        ent.Comp.LinkedPhotocopier = args.Sink;
        SendResults(ent);
    }

    private void OnPortDisconnected(Entity<MicroFiberAnalyzerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != MicroFiberAnalyzerUi.ReportSourcePort.Id)
            return;

        // The link system raises this event before removing the old link from its tables.
        ent.Comp.LinkedPhotocopier = null;
        SendResults(ent);
    }

    private void UpdatePrinterStatus(Entity<MicroFiberAnalyzerComponent> ent)
    {
        ResolveLinkedPhotocopier(ent);

        if (ent.Comp.PrinterStatus != GetPrinterStatus(ent))
            SendResults(ent);
    }

    private void ResolveLinkedPhotocopier(Entity<MicroFiberAnalyzerComponent> ent)
    {
        if (ent.Comp.LinkedPhotocopier != null && IsLinkedPhotocopier(ent, ent.Comp.LinkedPhotocopier.Value))
            return;

        ent.Comp.LinkedPhotocopier = null;
        ent.Comp.LinkedPhotocopier = FindLinkedPhotocopier(ent);
    }

    private EntityUid? FindLinkedPhotocopier(Entity<MicroFiberAnalyzerComponent> ent)
    {
        foreach (var printer in _deviceLink.GetLinkedSinks((ent.Owner, null),
            MicroFiberAnalyzerUi.ReportSourcePort))
        {
            if (!IsLinkedPhotocopier(ent, printer))
                continue;

            return printer;
        }

        return null;
    }

    private bool IsLinkedPhotocopier(Entity<MicroFiberAnalyzerComponent> ent, EntityUid printer)
    {
        if (TerminatingOrDeleted(printer))
            return false;

        if (!HasComp<PhotocopierComponent>(printer))
            return false;

        if (!HasComp<DeviceLinkSinkComponent>(printer))
            return false;

        return _deviceLink.GetLinks(ent, printer).Contains(
            (MicroFiberAnalyzerUi.ReportSourcePort, MicroFiberAnalyzerUi.ReportSinkPort));
    }

    private MicroFiberAnalyzerPrinterStatus GetPrinterStatus(Entity<MicroFiberAnalyzerComponent> ent)
    {
        if (ent.Comp.LinkedPhotocopier == null)
            return MicroFiberAnalyzerPrinterStatus.Disconnected;

        var printer = ent.Comp.LinkedPhotocopier.Value;
        if (!IsLinkedPhotocopier(ent, printer))
            return MicroFiberAnalyzerPrinterStatus.Disconnected;

        return _photocopier.CanPrintReport(printer)
            ? MicroFiberAnalyzerPrinterStatus.Ready
            : MicroFiberAnalyzerPrinterStatus.Unavailable;
    }

    private void OnInserted(Entity<MicroFiberAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlot)
            return;

        ent.Comp.PendingDescription = string.Empty;
        SendResults(ent);
    }

    private void OnRemoved(Entity<MicroFiberAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlot)
            return;

        if (ent.Comp.AnalyzingSwab == args.Entity)
            CancelAnalysis(ent);

        ent.Comp.PendingDescription = string.Empty;
        SendResults(ent);
    }

    private void ResetAnalysis(Entity<MicroFiberAnalyzerComponent> ent)
    {
        ent.Comp.AnalyzingSwab = null;
        ent.Comp.AnalysisStartedAt = TimeSpan.Zero;
        ent.Comp.AnalysisFinishAt = TimeSpan.Zero;
    }

    private void CancelAnalysis(Entity<MicroFiberAnalyzerComponent> ent)
    {
        ResetAnalysis(ent);
        _audio.PlayPvs(ent.Comp.SoundDeny, ent);
    }

    private void OnUiMessageAttempt(Entity<MicroFiberAnalyzerComponent> ent,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (!Equals(args.UiKey, MicroFiberAnalyzerUiKey.Key))
            return;

        if (!CanAccess(ent, args.Actor))
            args.Cancel();
    }

    private void OnOpened(Entity<MicroFiberAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, MicroFiberAnalyzerUiKey.Key))
            return;

        SendResults(ent, args.Actor);
    }

    private void OnRequestResults(Entity<MicroFiberAnalyzerComponent> ent,
        ref MicroFiberAnalyzerRequestResultsMessage args)
    {
        SendResults(ent, args.Actor);
    }

    private void OnEject(Entity<MicroFiberAnalyzerComponent> ent, ref MicroFiberAnalyzerEjectMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return;

        if (!_itemSlots.TryEjectToHands(ent, slot, args.Actor))
            return;
    }

    private void OnAnalyze(Entity<MicroFiberAnalyzerComponent> ent, ref MicroFiberAnalyzerAnalyzeMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (!IsDescriptionValid(args.Description))
            return;

        if (ent.Comp.AnalyzingSwab != null)
            return;

        if (!TryGetRequestedSample(ent, args.Swab, out var sample))
            return;

        ent.Comp.PendingDescription = args.Description;
        ent.Comp.AnalyzingSwab = sample.Owner;
        ent.Comp.AnalysisStartedAt = _timing.CurTime;
        ent.Comp.AnalysisFinishAt = _timing.CurTime + ent.Comp.AnalysisDuration;
        SendResults(ent);
    }

    private void OnSetDescription(Entity<MicroFiberAnalyzerComponent> ent,
        ref MicroFiberAnalyzerSetDescriptionMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (!IsDescriptionValid(args.Description))
            return;

        if (args.SampleId == null)
        {
            SetPendingDescription(ent, args.Swab, args.Description);
            return;
        }

        SetStoredDescription(ent, args.SampleId.Value, args.Description);
    }

    private static bool IsDescriptionValid(string? description)
    {
        if (description == null)
            return false;

        if (description.Length > MicroFiberAnalyzerUi.MaxDescriptionLength)
            return false;

        return true;
    }

    private void SetPendingDescription(
        Entity<MicroFiberAnalyzerComponent> ent,
        NetEntity? requestedSwab,
        string description)
    {
        if (!TryGetRequestedSample(ent, requestedSwab, out _))
            return;

        ent.Comp.PendingDescription = description;
        SendResults(ent);
    }

    private void SetStoredDescription(Entity<MicroFiberAnalyzerComponent> ent, int sampleId, string description)
    {
        var index = ent.Comp.History.FindIndex(entry => entry.Id == sampleId);
        if (index < 0)
            return;

        var stored = ent.Comp.History[index];
        ent.Comp.History[index] = stored.WithDescription(description);
        SendResults(ent);
    }

    private void OnPrint(Entity<MicroFiberAnalyzerComponent> ent, ref MicroFiberAnalyzerPrintMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (!TryGetPrintSample(ent, args.SampleId, out var sample))
            return;

        if (!TryGetReportPrinter(ent, args.Actor, out var printer))
            return;

        QueueReport(ent, args.Actor, printer, ent.Comp.MachineOutput,
            Loc.GetString("micro-fiber-analyzer-report-title"), CreateReport(sample));
    }

    private void OnPrintComparison(
        Entity<MicroFiberAnalyzerComponent> ent,
        ref MicroFiberAnalyzerPrintComparisonMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (args.FirstSampleId == args.SecondSampleId)
            return;

        if (!TryGetPrintSample(ent, args.FirstSampleId, out var first))
            return;

        if (!TryGetPrintSample(ent, args.SecondSampleId, out var second))
            return;

        if (!TryGetReportPrinter(ent, args.Actor, out var printer))
            return;

        QueueReport(ent, args.Actor, printer, ent.Comp.ComparisonOutput,
            Loc.GetString("micro-fiber-analyzer-comparison-report-title"), CreateComparisonReport(first, second));
    }

    private bool TryGetReportPrinter(
        Entity<MicroFiberAnalyzerComponent> ent,
        EntityUid actor,
        out EntityUid printer)
    {
        printer = default;
        if (_timing.CurTime < ent.Comp.PrintReadyAt)
        {
            NotifyPrinterUnavailable(ent, actor);
            return false;
        }

        if (GetPrinterStatus(ent) != MicroFiberAnalyzerPrinterStatus.Ready)
        {
            NotifyPrinterUnavailable(ent, actor);
            return false;
        }

        if (ent.Comp.LinkedPhotocopier == null)
            return false;

        printer = ent.Comp.LinkedPhotocopier.Value;
        return true;
    }

    private void QueueReport(
        Entity<MicroFiberAnalyzerComponent> ent,
        EntityUid actor,
        EntityUid printer,
        EntProtoId prototype,
        string title,
        string content)
    {
        if (!_photocopier.TryQueueReport(printer, prototype, title, content))
        {
            SendResults(ent, actor);
            return;
        }

        ent.Comp.PrintReadyAt = _timing.CurTime + ent.Comp.PrintCooldown;
        SendResults(ent);
    }

    private string CreateComparisonReport(
        MicroFiberAnalyzerHistoryEntry first,
        MicroFiberAnalyzerHistoryEntry second)
    {
        var text = new StringBuilder();
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-comparison-report-header"));
        text.AppendLine();
        AppendComparisonSample(text, first, 1);
        AppendComparisonSample(text, second, 2);
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-comparison-report-matches"));
        var gloveMatches = GloveMicroFiberComparison.FindGloveMatches(first.GloveMicroFibers, second.GloveMicroFibers);
        var storageMatches = GloveMicroFiberComparison.FindStorageMatches(first.StorageMicroFibers, second.StorageMicroFibers);
        AppendGloveResults(text, gloveMatches,
            emptyKey: "micro-fiber-analyzer-interface-compare-no-profiles");
        AppendStorageResults(text, storageMatches,
            emptyKey: "micro-fiber-analyzer-interface-compare-no-storage-micro-fibers");
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-interface-compare-note"));
        text.AppendLine();
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-footer"));
        return text.ToString();
    }

    private void AppendComparisonSample(
        StringBuilder text,
        MicroFiberAnalyzerHistoryEntry sample,
        int number)
    {
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-comparison-report-sample",
            ("number", number), ("identifier", FormattedMessage.EscapeText(sample.Identifier))));
        if (!string.IsNullOrEmpty(sample.SourceName))
            text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-source",
                ("source", FormattedMessage.EscapeText(sample.SourceName))));

        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-analyzed-at",
            ("time", MicroFiberAnalyzerUi.FormatRoundTime(sample.AnalyzedAt))));
        AppendReportDescription(text, sample.Description);
        text.AppendLine();
    }

    private void NotifyPrinterUnavailable(Entity<MicroFiberAnalyzerComponent> ent, EntityUid actor)
    {
        _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent, actor);
        SendResults(ent, actor);
    }

    /// <summary>
    /// Formats the official security report, escaping sample values while preserving the template's markup.
    /// </summary>
    private string CreateReport(MicroFiberAnalyzerHistoryEntry sample)
    {
        var text = new StringBuilder();
        AppendReportHeader(text, sample);
        AppendGloveResults(text, sample.GloveMicroFibers);
        AppendStorageResults(text, sample.StorageMicroFibers);
        AppendReportDescription(text, sample.Description);
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-footer"));
        return text.ToString();
    }

    private void AppendReportHeader(StringBuilder text, MicroFiberAnalyzerHistoryEntry sample)
    {
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-header",
            ("identifier", FormattedMessage.EscapeText(sample.Identifier))));
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-analyzed-at",
            ("time", MicroFiberAnalyzerUi.FormatRoundTime(sample.AnalyzedAt))));
        if (!string.IsNullOrEmpty(sample.SourceName))
            text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-source",
                ("source", FormattedMessage.EscapeText(sample.SourceName))));

        text.AppendLine();
    }

    private void AppendGloveResults(
        StringBuilder text,
        List<GloveMicroFiberTrace> traces,
        string emptyKey = "micro-fiber-analyzer-interface-no-traces")
    {
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-results",
            ("count", traces.Count)));
        text.AppendLine();

        if (traces.Count == 0)
        {
            text.AppendLine(Loc.GetString(emptyKey));
            text.AppendLine();
        }

        for (var i = 0; i < traces.Count; i++)
        {
            var trace = traces[i];
            var name = _prototype.TryIndex(trace.Prototype, out var prototype)
                ? prototype.Name
                : Loc.GetString("micro-fiber-analyzer-interface-unknown-type");
            text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-trace",
                ("number", i + 1), ("type", FormattedMessage.EscapeText(name)),
                ("code", FormattedMessage.EscapeText(trace.Code))));
            text.AppendLine();
        }
    }

    private void AppendStorageResults(
        StringBuilder text,
        List<StorageMicroFiberTrace> traces,
        string? emptyKey = null)
    {
        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-storage-micro-fibers",
            ("count", traces.Count)));
        if (traces.Count == 0 && emptyKey != null)
            text.AppendLine(Loc.GetString(emptyKey));

        for (var i = 0; i < traces.Count; i++)
        {
            var trace = traces[i];
            text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-storage-micro-fiber",
                ("number", i + 1), ("type", FormattedMessage.EscapeText(GetStorageMicroFiberName(trace))),
                ("code", FormattedMessage.EscapeText(trace.Code))));
            text.AppendLine();
        }
        text.AppendLine();
    }

    private void AppendReportDescription(StringBuilder text, string description)
    {
        if (string.IsNullOrEmpty(description))
            return;

        text.AppendLine(Loc.GetString("micro-fiber-analyzer-report-description",
            ("description", FormattedMessage.EscapeText(description))));
        text.AppendLine();
    }

    private string GetStorageMicroFiberName(StorageMicroFiberTrace trace)
    {
        if (!string.IsNullOrEmpty(trace.SourceName))
            return trace.SourceName;

        if (_prototype.TryIndex(trace.Prototype, out var prototype))
            return prototype.Name;

        return Loc.GetString("micro-fiber-analyzer-interface-unknown-item");
    }

    private bool TryGetPrintSample(Entity<MicroFiberAnalyzerComponent> ent, int? sampleId,
        out MicroFiberAnalyzerHistoryEntry sample)
    {
        sample = null!;
        if (sampleId == null)
            return false;

        var stored = ent.Comp.History.Find(entry => entry.Id == sampleId.Value);
        if (stored == null)
            return false;

        sample = stored;
        return true;
    }

    private bool TryGetRequestedSample(
        Entity<MicroFiberAnalyzerComponent> ent,
        NetEntity? requestedSwab,
        out Entity<GloveMicroFiberSwabComponent> sample)
    {
        if (!TryGetSample(ent, out sample))
            return false;

        if (!sample.Comp.Used)
            return false;

        if (requestedSwab == null)
            return false;

        return requestedSwab.Value == GetNetEntity(sample.Owner);
    }

    private bool TryGetSample(Entity<MicroFiberAnalyzerComponent> ent,
        out Entity<GloveMicroFiberSwabComponent> sample)
    {
        sample = default;
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return false;

        if (slot.Item == null)
            return false;

        if (TerminatingOrDeleted(slot.Item.Value))
            return false;

        if (!TryComp<GloveMicroFiberSwabComponent>(slot.Item.Value, out var swab))
            return false;

        sample = (slot.Item.Value, swab);
        return true;
    }

    private bool CanAccess(Entity<MicroFiberAnalyzerComponent> ent, EntityUid actor)
    {
        var range = _ui.GetUiRange(ent.Owner, MicroFiberAnalyzerUiKey.Key);
        if (range <= 0)
            return _interaction.IsAccessible(actor, ent.Owner);

        if (HasComp<IgnoreUIRangeComponent>(actor))
            return true;

        return _interaction.InRangeAndAccessible(actor, ent.Owner, range);
    }

    private void SendResults(Entity<MicroFiberAnalyzerComponent> ent)
    {
        var results = GetResults(ent);
        foreach (var actor in _ui.GetActors(ent.Owner, MicroFiberAnalyzerUiKey.Key))
        {
            if (!CanAccess(ent, actor))
                continue;

            _ui.ServerSendUiMessage(ent.Owner, MicroFiberAnalyzerUiKey.Key, results, actor);
        }
    }

    private void SendResults(Entity<MicroFiberAnalyzerComponent> ent, EntityUid actor)
    {
        if (!CanAccess(ent, actor))
            return;

        _ui.ServerSendUiMessage(ent.Owner, MicroFiberAnalyzerUiKey.Key, GetResults(ent), actor);
    }

    private MicroFiberAnalyzerResultsMessage GetResults(Entity<MicroFiberAnalyzerComponent> ent)
    {
        List<MicroFiberAnalyzerHistoryEntry> history = [.. ent.Comp.History];
        var hasSwab = TryGetSample(ent, out var sample);
        var used = hasSwab && sample.Comp.Used;
        ent.Comp.PrinterStatus = GetPrinterStatus(ent);
        return new MicroFiberAnalyzerResultsMessage
        {
            HasSwab = hasSwab,
            Used = used,
            PrintReadyAt = ent.Comp.PrintReadyAt,
            PrintCooldown = ent.Comp.PrintCooldown,
            History = history,
            IsAnalyzing = ent.Comp.AnalyzingSwab != null,
            AnalysisStartedAt = ent.Comp.AnalysisStartedAt,
            AnalysisFinishAt = ent.Comp.AnalysisFinishAt,
            PendingDescription = ent.Comp.PendingDescription,
            PendingSwab = hasSwab ? GetNetEntity(sample.Owner) : null,
            PrinterStatus = ent.Comp.PrinterStatus,
            AnalyzerId = ent.Owner.Id,
            PendingSourceName = hasSwab ? sample.Comp.SourceName : string.Empty,
        };
    }
}
