// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MouthContainer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MouthContainerComponent : Component
{
    #region Mouthslot

    [ViewVariables]
    public ContainerSlot MouthSlot = default!;

    [DataField]
    public string MouthSlotId = "mouth-slot";

    #endregion

    #region Actions

    [DataField]
    public EntProtoId EjectAction = "ActionMouthContainerSpit";

    [DataField, AutoNetworkedField]
    public EntityUid? EjectActionEntity;

    #endregion

    #region Whitelists

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    #endregion

    #region Locales

    [DataField]
    public LocId InsertVerbIn = "insert-to-mouth-in";

    [DataField]
    public LocId InsertVerbOut = "insert-to-mouth-out";

    [DataField]
    public LocId EjectVerbOut = "eject-from-mouth-out";

    [DataField]
    public LocId InsertMessage = "insert-to-mouth-success";

    [DataField]
    public LocId EjectMessage = "eject-from-mouth-success";

    #endregion

    #region Do-After-Durations

    [DataField]
    public TimeSpan InsertUserDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan EjectUserDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan InsertDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan EjectDuration = TimeSpan.Zero;

    #endregion
}
