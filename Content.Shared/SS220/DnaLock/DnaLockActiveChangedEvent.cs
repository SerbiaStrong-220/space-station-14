// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.DnaLock;

/// <summary>
/// Raised on an item when its DNA lock is turned on or off.
/// </summary>
[ByRefEvent]
public readonly record struct DnaLockActiveChangedEvent(bool Active);
