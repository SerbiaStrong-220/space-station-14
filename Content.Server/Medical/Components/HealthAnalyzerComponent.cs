using Content.Server.SS220.Medical;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Medical.Components;

/// <summary>
/// After scanning, retrieves the target Uid to use with its related UI.
/// </summary>
/// <remarks>
/// Requires <c>ItemToggleComponent</c>.
/// </remarks>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(HealthAnalyzerSystem), typeof(CryoPodSystem), typeof(HealthAnalyzerPrintSystem))] // SS220-health-analyzer-report
public sealed partial class HealthAnalyzerComponent : Component
{
    /// <summary>
    /// When should the next update be sent for the patient
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The delay between patient health updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long it takes to scan someone.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// Which entity has been scanned, for continuous updates
    /// </summary>
    [DataField]
    public EntityUid? ScannedEntity;

    /// <summary>
    /// The maximum range in tiles at which the analyzer can receive continuous updates, a value of null will be infinite range
    /// </summary>
    [DataField]
    public float? MaxScanRange = 2.5f;

    /// <summary>
    /// Sound played on scanning begin
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    /// Sound played on scanning end
    /// </summary>
    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Whether to show up the popup
    /// </summary>
    [DataField]
    public bool Silent;

    // SS220-health-analyzer-report - bgn
    /// <summary>
    /// Sound that's played when printing a scan report
    /// </summary>
    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// Whether this analyzer can print scan reports
    /// </summary>
    [DataField]
    public bool CanPrint;

    /// <summary>
    /// The paper entity spawned when printing a report
    /// </summary>
    [DataField]
    public EntProtoId MachineOutput = "HealthAnalyzerReportPaper";

    /// <summary>
    /// Name of the last scanned patient, used for report titles
    /// </summary>
    public string LastScannedName = string.Empty;

    /// <summary>
    /// Last generated report contents available for printing
    /// </summary>
    public string LastScannedReport = string.Empty;

    /// <summary>
    /// Cooldown between print attempts.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Next time when report printing becomes available.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;
    // SS220-health-analyzer-report - end
}
