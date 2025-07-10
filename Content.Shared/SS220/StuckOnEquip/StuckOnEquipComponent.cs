// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StuckOnEquip;

/// <summary>
/// Literally like <see cref="UnremovableComponent"/>, but potentially more flexible
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StuckOnEquipComponent : Component
{
    /// <summary>
    /// If true, the item will be able to be locked in hand, if false entity will be locked only in the slot
    /// </summary>
    [DataField]
    public bool InHandItem = false;

    /// <summary>
    /// If true, drop blocked entities upon the death of the owner
    /// </summary>
    [DataField]
    public bool ShouldDropOnDeath = true;

    /// <summary>
    /// If true, the item isalready stuck in the slot or hand
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsStuck = false;
}
