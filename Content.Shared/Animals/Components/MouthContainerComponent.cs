using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Animals.Components;

[RegisterComponent]
public sealed partial class MouthContainerComponent : Component
{
    [ViewVariables]
    public ContainerSlot MouthSlot = default!;

    public bool IsVisibleCheeks;

    [ViewVariables]
    public readonly string MouthSlotId = "mouth-slot";

    [DataField]
    public EntityWhitelist? Whitelist;
    [DataField]
    public EntityWhitelist? Priority;
    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public LocId InsertVerbIn = "insert-to-mouth-in";
    [DataField]
    public LocId EjectVerbIn = "eject-from-mouth-in";
    [DataField]
    public LocId InsertVerbOut = "insert-to-mouth-out";
    [DataField]
    public LocId EjectVerbOut = "eject-from-mouth-out";
    [DataField]
    public LocId InsertMessage = "insert-to-mouth-success";
    [DataField]
    public LocId EjectMessage = "eject-from-mouth-success";

    [DataField]
    public float InsertDuration = 1f;
}
