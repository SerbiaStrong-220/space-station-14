// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StuckOnEquip;

[RegisterComponent, NetworkedComponent]
public sealed partial class StuckOnEquipComponent : Component
{
    /// <summary>
    /// If true, the item will be locked in hand, if false, entity will be locked in the slot
    /// </summary>
    [DataField]
    public bool InHandItem = false;

    /// <summary>
    /// If true, drop blocked entities upon the death of the owner
    /// </summary>
    [DataField]
    public bool ShouldDropOnDeath = true;
}
