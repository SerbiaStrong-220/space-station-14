// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Completes scanning in the mode selected when the scan began.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class GloveMicroFiberScannerDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Mode at the start of this scan.
    /// </summary>
    [DataField]
    public GloveMicroFiberScannerMode Mode;

    public override DoAfterEvent Clone()
    {
        return (GloveMicroFiberScannerDoAfterEvent)MemberwiseClone();
    }
}
