// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ClinkGlasses;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedClinkGlassesSystem))]
public sealed partial class ClinkGlassesReceiverComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public EntityUid Initiator;

    [ViewVariables(VVAccess.ReadOnly)]
    public float ReceiveRange = 2f;

    /// <summary>
    ///     Default value in seconds for <see cref="LifeTime"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float BaseLifeTime = 1500.0f;

    /// <summary>
    ///     LifeTime in seconds. When it's less than zero, this component should be removed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LifeTime = TimeSpan.Zero;
}
