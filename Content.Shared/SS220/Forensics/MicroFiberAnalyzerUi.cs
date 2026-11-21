// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Globalization;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Shared analyzer limits and formatting for sample descriptions and history.
/// </summary>
public static class MicroFiberAnalyzerUi
{
    /// <summary>
    /// The analyzer port used to establish a report printer link.
    /// </summary>
    public static readonly ProtoId<SourcePortPrototype> ReportSourcePort = "MicroFiberReport";

    /// <summary>
    /// The copier port accepting report printer links.
    /// </summary>
    public static readonly ProtoId<SinkPortPrototype> ReportSinkPort = "PrintDocument";

    /// <summary>
    /// Maximum description length accepted by the editor and server.
    /// </summary>
    public const int MaxDescriptionLength = 512;

    /// <summary>
    /// Formats an archive sample's name from its permanent identifier.
    /// </summary>
    public static string GetSampleName(MicroFiberAnalyzerHistoryEntry sample)
    {
        return Loc.GetString("micro-fiber-analyzer-interface-history-sample", ("identifier", sample.Identifier));
    }

    /// <summary>
    /// Formats a recorded round duration as hours, minutes and seconds without wrapping after a day.
    /// </summary>
    public static string FormatRoundTime(TimeSpan roundTime)
    {
        if (roundTime < TimeSpan.Zero)
            roundTime = TimeSpan.Zero;

        var hours = roundTime.Ticks / TimeSpan.TicksPerHour;
        return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}",
            hours, roundTime.Minutes, roundTime.Seconds);
    }
}

/// <summary>
/// The interface used to examine a swab inserted into the microfiber analyzer.
/// </summary>
[Serializable, NetSerializable]
public enum MicroFiberAnalyzerUiKey : byte
{
    Key,
}

/// <summary>
/// The linked copier's ability to receive a report from this analyzer.
/// </summary>
[Serializable, NetSerializable]
public enum MicroFiberAnalyzerPrinterStatus : byte
{
    Disconnected,
    Unavailable,
    Ready,
}

/// <summary>
/// A stored snapshot of a physical sample previously inserted into this analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerHistoryEntry
{
    /// <summary>
    /// The sample's permanent number within this analyzer.
    /// </summary>
    public readonly int Id;

    /// <summary>
    /// The glove identities and types captured when analysis completed.
    /// </summary>
    public readonly List<GloveMicroFiberTrace> GloveMicroFibers;

    /// <summary>
    /// The individual material profiles captured when analysis completed.
    /// </summary>
    public readonly List<StorageMicroFiberTrace> StorageMicroFibers;

    /// <summary>
    /// The investigator's description of this analysis record.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The round, analyzer UID and local history number identifying this completed analysis.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// The scanned item's name captured on the physical sample.
    /// </summary>
    public readonly string SourceName;

    /// <summary>
    /// Elapsed round time when this sample's analysis completed, retained as a permanent snapshot.
    /// </summary>
    public readonly TimeSpan AnalyzedAt;

    public MicroFiberAnalyzerHistoryEntry(int id, List<GloveMicroFiberTrace> gloveMicroFibers,
        string description = "", string identifier = "", string sourceName = "",
        List<StorageMicroFiberTrace>? storageMicroFibers = null, TimeSpan analyzedAt = default)
    {
        Id = id;
        GloveMicroFibers = gloveMicroFibers;
        StorageMicroFibers = storageMicroFibers ?? [];
        Description = description;
        Identifier = identifier;
        SourceName = sourceName;
        AnalyzedAt = analyzedAt;
    }

    /// <summary>
    /// Replaces the description without changing the evidence or previously sent history snapshots.
    /// </summary>
    public MicroFiberAnalyzerHistoryEntry WithDescription(string description)
    {
        return new MicroFiberAnalyzerHistoryEntry(Id, GloveMicroFibers,
            description: description,
            identifier: Identifier,
            sourceName: SourceName,
            storageMicroFibers: StorageMicroFibers,
            analyzedAt: AnalyzedAt);
    }
}

/// <summary>
/// Private completed analyses and the pending sample's status, sent only to users of the interface.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerResultsMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The server-side analyzer UID used in completed sample identifiers.
    /// </summary>
    public required int AnalyzerId { get; init; }

    /// <summary>
    /// Whether the inserted swab contains a collected sample, including a sample without traces.
    /// </summary>
    public required bool Used { get; init; }

    /// <summary>
    /// Whether a swab is physically inserted and can be ejected.
    /// </summary>
    public required bool HasSwab { get; init; }

    /// <summary>
    /// When the printer can produce another report.
    /// </summary>
    public required TimeSpan PrintReadyAt { get; init; }

    /// <summary>
    /// Delay between printed reports.
    /// </summary>
    public required TimeSpan PrintCooldown { get; init; }

    /// <summary>
    /// This analyzer's stored samples, sent only to authorized users of the interface.
    /// </summary>
    public required List<MicroFiberAnalyzerHistoryEntry> History { get; init; }

    /// <summary>
    /// Whether the analyzer is processing the physically inserted swab.
    /// </summary>
    public required bool IsAnalyzing { get; init; }

    /// <summary>
    /// When the current analysis started.
    /// </summary>
    public required TimeSpan AnalysisStartedAt { get; init; }

    /// <summary>
    /// When the current analysis will finish if the swab stays inserted.
    /// </summary>
    public required TimeSpan AnalysisFinishAt { get; init; }

    /// <summary>
    /// The description to attach to the pending sample when its analysis completes.
    /// </summary>
    public required string PendingDescription { get; init; }

    /// <summary>
    /// The physical swab targeted by pending analysis and description requests.
    /// </summary>
    public required NetEntity? PendingSwab { get; init; }

    /// <summary>
    /// The scanned item's name on the inserted sample, without revealing its unanalyzed material evidence.
    /// </summary>
    public string PendingSourceName { get; init; } = string.Empty;

    /// <summary>
    /// Whether a valid linked copier is ready to print a report.
    /// </summary>
    public required MicroFiberAnalyzerPrinterStatus PrinterStatus { get; init; }
}

/// <summary>
/// Requests the currently inserted sample's results after opening the analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerRequestResultsMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Requests removal of the swab from the analyzer into the user's hands.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerEjectMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Starts processing the inserted sample, including the description currently entered by the user.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerAnalyzeMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The description entered for the pending sample.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The expected physical sample, preventing a delayed request from analyzing a replacement swab.
    /// </summary>
    public readonly NetEntity? Swab;

    public MicroFiberAnalyzerAnalyzeMessage(string description, NetEntity? swab)
    {
        Description = description;
        Swab = swab;
    }
}

/// <summary>
/// Saves a description for a completed analysis or, when the ID is null, for the pending physical sample.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerSetDescriptionMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The completed record to edit, or null to edit the pending physical sample.
    /// </summary>
    public readonly int? SampleId;

    /// <summary>
    /// The new description, including an empty string to clear it.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The expected physical sample when editing a pending description.
    /// </summary>
    public readonly NetEntity? Swab;

    public MicroFiberAnalyzerSetDescriptionMessage(int? sampleId, string description, NetEntity? swab = null)
    {
        SampleId = sampleId;
        Description = description;
        Swab = swab;
    }
}

/// <summary>
/// Requests a paper report of a completed analysis belonging to this analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerPrintMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The stored sample to print. Requests without a stored sample ID are rejected.
    /// </summary>
    public readonly int? SampleId;

    public MicroFiberAnalyzerPrintMessage(int? sampleId = null)
    {
        SampleId = sampleId;
    }
}

/// <summary>
/// Requests a separate comparison report of two completed samples belonging to this analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicroFiberAnalyzerPrintComparisonMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The first stored sample to compare.
    /// </summary>
    public readonly int FirstSampleId;

    /// <summary>
    /// The second stored sample to compare; it must differ from the first.
    /// </summary>
    public readonly int SecondSampleId;

    public MicroFiberAnalyzerPrintComparisonMessage(int firstSampleId, int secondSampleId)
    {
        FirstSampleId = firstSampleId;
        SecondSampleId = secondSampleId;
    }
}
