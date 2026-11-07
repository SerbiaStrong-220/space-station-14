// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// Added to toggleable clothing while it holds the item that was worn in the slot before its attached clothing was put on.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ToggleableClothingReplaceSystem))]
public sealed partial class ToggleableClothingReplacedComponent : Component
{
    public const string ContainerId = "toggleable-clothing-replaced";

    [DataField, AutoNetworkedField]
    public EntityUid Wearer;
}

/// <summary>
/// Raised on toggleable clothing when its attached clothing is toggled on but the slot is already occupied.
/// </summary>
[ByRefEvent]
public record struct ToggleableClothingSlotOccupiedEvent(EntityUid User, EntityUid Wearer, bool Handled = false);
