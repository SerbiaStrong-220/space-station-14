
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Attachables;

[RegisterComponent, NetworkedComponent]
public sealed partial class AttachablesContainerComponent : Component
{
    [DataField("slots")]
    public Dictionary<string, ItemSlot> AllowedSlots = new();

    public EntityUid? ActiveSlot;
}
