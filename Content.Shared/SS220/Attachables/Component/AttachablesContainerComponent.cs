
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Attachables;

[RegisterComponent, NetworkedComponent]
public sealed partial class AttachablesContainerComponent : Component
{
    [DataField("slots")]
    public Dictionary<string, ItemSlot> AllowedSlots = new();

    public EntityUid? ActiveSlot;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleAttachablesMenu";

    [DataField]
    public EntityUid? ToggleActionEntity;
}

public sealed partial class AttachablesContainerOpenEvent : InstantActionEvent;
