// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Chameleon;

/// <summary>
///     Reveals the chameleon when raised.
/// </summary>
[ByRefEvent]
public record struct ChameleonRevealEvent();

/// <summary>
///     Raised whenever ambient music tries to play.
/// </summary>
/// <param name="Proto">Prototype applied to the chameleon</param>
/// <param name="Cancelled">Cancels Chameleon applying if true</param>
[ByRefEvent]
public record struct ChameleonAttemptEvent(EntProtoId Proto, bool Cancelled = false);
