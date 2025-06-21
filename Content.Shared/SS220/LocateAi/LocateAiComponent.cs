using Robust.Shared.GameStates;

namespace Content.Shared.SS220.LocateAi;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class LocateAiComponent : Component
{
    [DataField]
    public float RangeDetection = 10f;

    public bool IsActive = true;

    public bool LastDetected = false;
}
