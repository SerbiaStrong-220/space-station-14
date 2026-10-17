// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.CentComm;

/// <summary>
/// Added to the eye a <see cref="CentCommEyeConsoleComponent"/> spawned, which is what the operator
/// is attached to while observing.
[RegisterComponent]
public sealed partial class CentCommEyeUserComponent : Component
{
    /// <summary>
    /// The console this eye belongs to.
    /// </summary>
    [ViewVariables]
    public EntityUid Console;

    /// <summary>
    /// "Return to body" action.
    /// </summary>
    [ViewVariables]
    public EntityUid? ReturnActionEntity;

    /// <summary>
    /// Radio channel filter action
    /// </summary>
    [ViewVariables]
    public EntityUid? ChannelsActionEntity;
}
