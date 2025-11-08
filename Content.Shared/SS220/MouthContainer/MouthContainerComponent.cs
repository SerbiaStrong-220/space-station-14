using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.MouthContainer;

[RegisterComponent]
public sealed partial class MouthContainerComponent : Component
{
    /// <summary>
    ///     MouthSlot.
    /// </summary>
    [ViewVariables]
    public ContainerSlot MouthSlot = default!;
    [DataField]
    public string MouthSlotId = "mouth-slot";

    /// <summary>
    ///     Whitelists.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///     Locales.
    /// </summary>
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

    /// <summary>
    ///     Do-After durations.
    /// </summary>
    [DataField]
    public float InsertDuration = 1f;
    [DataField]
    public float EjectDuration = 5f;
}
