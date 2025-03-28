// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Spray.Components;

/// <summary>
/// This is used for relaying solition events
/// to an entity in the user's clothing slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingSlotSolutionProviderComponent : Component
{
    /// <summary>
    /// The slot that the solution provider should be located in.
    /// </summary>
    [DataField("solutionRequiredSlot", required: true)]
    public SlotFlags SolutionRequiredSlot;

    /// <summary>
    /// A whitelist for determining whether or not an solution provider is valid.
    /// </summary>
    [DataField("solutionProviderWhitelist")]
    public EntityWhitelist? SolutionProviderWhitelist;

    [DataField]
    public string TankSolutionName = "tank";

    [DataField]
    public string NozzleSolutionName = "spray";

}
