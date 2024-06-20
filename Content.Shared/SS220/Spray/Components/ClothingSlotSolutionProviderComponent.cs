using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Content.Shared.SS220.Spray.System;

namespace Content.Shared.SS220.Spray.Components;

/// <summary>
/// This is used for relaying solition events
/// to an entity in the user's clothing slot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSpraySystem))]
public sealed partial class ClothingSlotSolutionProviderComponent : SolutionProviderComponent
{
    /// <summary>
    /// The slot that the ammo provider should be located in.
    /// </summary>
    [DataField("requiredSlot", required: true)]
    public SlotFlags SolutionRequiredSlot;

    /// <summary>
    /// A whitelist for determining whether or not an solution provider is valid.
    /// </summary>
    [DataField("solutionProviderWhitelist")]
    public EntityWhitelist? SolutionProviderWhitelist;
}
