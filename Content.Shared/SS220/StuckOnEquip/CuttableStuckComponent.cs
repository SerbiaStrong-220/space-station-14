// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StuckOnEquip;

/// <summary>
/// Allows a stuck item to be cut off with a held sharp tool, injuring its wearer upon successful removal.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CuttableStuckComponent : Component
{
    /// <summary>
    /// Time required to cut the item free.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Damage dealt to the wearer after successful removal, ignoring resistances.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();
}
