// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands.Components;

namespace Content.Client.SS220.Weapons.Ranged.Visualizer.Components;

/// <summary>
/// Sets which sprite RSI is used for displaying the gun visuals and what state to use based on the ammo count.
/// </summary>
[RegisterComponent]
public sealed partial class GunByHasAmmoVisualsComponent : Component
{
    /// <summary>
    ///     Layer to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField("inhandVisuals")]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [DataField("state")] public string? PreviousState;
    [ViewVariables] public  int? LayerNumber;
}
