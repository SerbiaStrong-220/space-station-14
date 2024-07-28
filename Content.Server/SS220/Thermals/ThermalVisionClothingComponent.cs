// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Server.SS220.Thermals;

/// <summary>
/// Adds ThermalComponent to the user when enabled, either by an action or the system's SetEnabled method.
/// </summary>
[RegisterComponent]
public sealed partial class ThermalVisionClothingComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ThermalVisionRadius = 8f;
}
