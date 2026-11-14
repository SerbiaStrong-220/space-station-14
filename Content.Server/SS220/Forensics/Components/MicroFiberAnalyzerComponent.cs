// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Stores the microfiber analyzer's sample slot, analysis progress, linked copier and private sample history.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class MicroFiberAnalyzerComponent : Component
{
    /// <summary>
    /// Stored snapshots of analyzed samples. This runtime history lasts for the lifetime of the analyzer.
    /// </summary>
    public readonly List<MicroFiberAnalyzerHistoryEntry> History = [];

    /// <summary>
    /// The copier linked to the analyzer's report output port, if any.
    /// </summary>
    public EntityUid? LinkedPhotocopier;

    /// <summary>
    /// Last printer availability sent to users of the private interface.
    /// </summary>
    public MicroFiberAnalyzerPrinterStatus PrinterStatus;

    /// <summary>
    /// The slot accepting a glove-micro-fiber swab.
    /// </summary>
    [DataField]
    public string SampleSlot = "sampleSlot";

    /// <summary>
    /// How long an analysis takes before consuming the inserted sample and recording its results.
    /// </summary>
    [DataField]
    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The physical swab whose analysis is currently running, if any.
    /// </summary>
    public EntityUid? AnalyzingSwab;

    /// <summary>
    /// When the current analysis started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan AnalysisStartedAt;

    /// <summary>
    /// When the current analysis can finish.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan AnalysisFinishAt;

    /// <summary>
    /// User-written description of the inserted sample, saved when its analysis finishes.
    /// </summary>
    public string PendingDescription = string.Empty;

    /// <summary>
    /// The paper used for printed reports.
    /// </summary>
    [DataField]
    public EntProtoId MachineOutput = "MicroFiberReportPaper";

    /// <summary>
    /// The separate paper used for reports comparing two completed samples.
    /// </summary>
    [DataField]
    public EntProtoId ComparisonOutput = "MicroFiberComparisonReportPaper";

    /// <summary>
    /// Delay between printed reports.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When the printer can produce another report.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan PrintReadyAt;

    /// <summary>
    /// Sound played when removing the sample interrupts its analysis.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}
