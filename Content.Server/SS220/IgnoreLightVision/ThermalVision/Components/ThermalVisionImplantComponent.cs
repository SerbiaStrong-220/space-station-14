// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Thermals;

/// <summary>
/// Adds ThermalComponent to the user when implant action is used.
/// </summary>
[RegisterComponent]
public sealed partial class ThermalVisionImplantComponent : Component
{
    public bool IsActive = false;

    [DataField, ViewVariables]
    public float VisionRadius = 8f;
    [DataField, ViewVariables]
    public float CloseVisionRadius = 2f;
}
