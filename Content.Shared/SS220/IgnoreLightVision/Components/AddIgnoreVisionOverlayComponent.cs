// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.IgnoreLightVision;

public abstract partial class AddIgnoreLightVisionOverlayComponent : Component
{
    [DataField]
    public IgnoreLightVisionOverlayState State = IgnoreLightVisionOverlayState.Off;
    [DataField]
    public float VisionRadius = 8f;
    [DataField]
    public float HighSensitiveVisionRadius = 2f;
    [DataField]
    public bool AddAction = false;

    public AddIgnoreLightVisionOverlayComponent(float radius, float closeRadius)
    {
        VisionRadius = radius;
        HighSensitiveVisionRadius = closeRadius;
    }
}

[Serializable, NetSerializable]
public sealed class AddIgnoreLightVisionOverlayState : ComponentState
{
    public IgnoreLightVisionOverlayState State { get; init; }
    public float VisionRadius { get; init; }
    public float HighSensitiveVisionRadius { get; init; }
}

[Serializable, NetSerializable]
public enum IgnoreLightVisionOverlayState
{
    Error,
    Off,
    Half,
    Full
}
