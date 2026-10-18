// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.BodyCamera;

[RegisterComponent, NetworkedComponent]
public sealed partial class BodyCameraPowerComponent : Component
{
    /// <summary>
    /// Power consumed while transmitting video or recording audio, in watts.
    /// </summary>
    [DataField]
    public float DrawRate = 1f;
}

[ByRefEvent]
public readonly record struct BodyCameraActiveChangedEvent(bool Active);
