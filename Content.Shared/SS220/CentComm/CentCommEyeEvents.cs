// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;

namespace Content.Shared.SS220.CentComm;

/// <summary>
/// Raised on the operator when they use the granted action to stop piloting the eye.
/// </summary>
public sealed partial class CentCommEyeReturnActionEvent : InstantActionEvent;
