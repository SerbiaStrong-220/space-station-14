// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.ClinkGlasses;

[RegisterComponent]
[Access(typeof(SharedClinkGlassesSystem))]
public sealed partial class ClinkGlassesReceiverComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Initiator;

    [ViewVariables(VVAccess.ReadOnly)]
    public float ReceiveRange = 2f;

    /// <summary>
    ///     LifeTime in seconds. When its less than zero, this component should be removed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float LifeTime = 15.0f;
}
