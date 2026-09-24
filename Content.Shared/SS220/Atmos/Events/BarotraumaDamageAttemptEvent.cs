// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Atmos;

/// <summary>
/// An event that occurs before barotrauma damage is dealt.
/// </summary>
[ByRefEvent]
public sealed partial class BarotraumaDamageAttemptEvent : CancellableEntityEventArgs
{
}
