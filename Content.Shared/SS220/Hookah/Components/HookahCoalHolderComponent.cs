using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Hookah.Components;

[RegisterComponent]
public sealed partial class HookahCoalHolderComponent : Component
{
    public const string CoalSlotId = "coal_slot";

    [DataField("coal_slot")]
    public ItemSlot CoalSlot = new();

    [DataField]
    public SoundSpecifier LightSound =
        new SoundPathSpecifier("/Audio/Items/Lighters/lighter1.ogg");

    [DataField]
    public SoundSpecifier ExtinguishSound =
        new SoundPathSpecifier("/Audio/Effects/extinguish.ogg");
}
