using Content.Shared.Containers.ItemSlots;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ToggleableItemSlot;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class ToggleableItemSlotComponent : Component
{
    public const string HiddenSlot = "hidden_slot";

    [DataField]
    public ItemSlot HiddenItemSlot = new();

    [DataField]
    public ProtoId<ToolQualityPrototype> NeedTool = "Screwing";

    [DataField]
    public TimeSpan TimeToOpen = TimeSpan.FromSeconds(2f);

    [DataField]
    public SoundSpecifier? SoundOpen = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");

    [DataField]
    public SoundSpecifier? SoundClosed = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");
}
