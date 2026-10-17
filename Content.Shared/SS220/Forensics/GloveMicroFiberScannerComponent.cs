// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Selects which glove evidence the portable scanner collects.
/// </summary>
[Serializable, NetSerializable]
public enum GloveMicroFiberScannerMode : byte
{
    SurfaceTraces,
    OwnSample,
}

/// <summary>
/// A portable scanner that produces physical glove-trace samples for the analyzer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GloveMicroFiberScannerComponent : Component
{
    /// <summary>
    /// Whether to collect deposited traces or the target's own identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public GloveMicroFiberScannerMode Mode;

    /// <summary>
    /// Time required to scan a target.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Physical sample produced when scanning completes.
    /// </summary>
    [DataField]
    public EntProtoId SwabPrototype = "GloveMicroFiberSwab";
}
