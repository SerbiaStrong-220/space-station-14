// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.CentComm;

/// <summary>
/// Added to the body left at a <see cref="CentCommEyeConsoleComponent"/> while its owner is observing.
/// </summary>
[RegisterComponent]
public sealed partial class CentCommEyeOperatorComponent : Component
{
    /// <summary>
    /// The console this body is sitting at.
    /// </summary>
    [ViewVariables]
    public EntityUid Console;
}
