// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Server.SS220.Thermals;

/// <summary>
/// Adds ThermalComponent to the user when implant action is used.
/// </summary>
[RegisterComponent]
public sealed partial class ThermalVisionImplantComponent : Component
{
    [DataField]
    public bool IsActive = false;
    [DataField, ViewVariables]
    public float ThermalVisionRadius = 8f;
}
