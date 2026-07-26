using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Hookah.Components;

[RegisterComponent]
public sealed partial class SmokingFuelComponent : Component
{
    public const string TobaccoSlotId = "tobacco_slot";

    [DataField("tobacco_slot")]
    public ItemSlot TobaccoSlot = new();

    [DataField]
    public int TobaccoPuffs;

    [DataField]
    public EntProtoId TobaccoId = "LeavesTobaccoDried";

    [DataField]
    public int PuffsPerPack = 20;

    public float CoalTime;
}
