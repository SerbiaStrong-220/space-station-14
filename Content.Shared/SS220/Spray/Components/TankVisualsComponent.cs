// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
namespace Content.Shared.SS220.Spray.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankVisualsComponent : Component
{
    public const string TankNozzleSlot = "NozzleSlot";

    [DataField("tankSlot")]
    public ItemSlot TankSlot = new();

    [ViewVariables] public EntityUid? ContainedNozzle;
}
