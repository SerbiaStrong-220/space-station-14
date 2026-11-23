// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.StorageClothingBlocker;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class StorageClothingBlockerComponent : Component
{
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.TORSO;
}
