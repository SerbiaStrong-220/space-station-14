// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CentComm;

/// <summary>
/// A CentComm console that lets its operator pilot a remote observer, similiar to AI eye.
/// </summary>
[RegisterComponent]
public sealed partial class CentCommEyeConsoleComponent : Component
{
    /// <summary>
    /// Prototype spawned as the remote eye.
    /// </summary>
    [DataField]
    public EntProtoId EyeProto = "CentCommObserverEye";

    /// <summary>
    /// How far the operator may drift from the console before they are kicked out of the eye.
    /// </summary>
    [DataField]
    public float MaxUserRange = 1.5f;

    /// <summary>
    /// This console's eye. It is spawned on first use and then kept for the console's lifetime.
    /// </summary>
    [ViewVariables]
    public EntityUid? Eye;

    /// <summary>
    /// The operator currently piloting <see cref="Eye"/>, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? User;
}
