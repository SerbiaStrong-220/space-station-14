// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Requests a collection mode for a scanner held by the sending player's character.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberScannerSetModeEvent : EntityEventArgs
{
    /// <summary>
    /// The held scanner whose collection mode should change.
    /// </summary>
    public readonly NetEntity Scanner;

    /// <summary>
    /// The requested collection mode.
    /// </summary>
    public readonly GloveMicroFiberScannerMode Mode;

    public GloveMicroFiberScannerSetModeEvent(NetEntity scanner, GloveMicroFiberScannerMode mode)
    {
        Scanner = scanner;
        Mode = mode;
    }
}
