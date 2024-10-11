// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Thermals;

/// <summary>
/// Adds ThermalComponent to the user when enabled, either by an action or the system's SetEnabled method.
/// </summary>
[RegisterComponent]
public sealed partial class ThermalVisionClothingComponent : Component
{
    [DataField]
    public float VisionRadius = 8f;
    [DataField]
    public float CloseVisionRadius = 2f;
}
