// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Zones.Components;

/// <summary>
/// The component used to determine the zones located on the <see cref="ZoneParams.Container"/>.
/// A zone can be used to determine a certain area on the <see cref="ZoneParams.Container"/>
/// in which various events can occur, as well as with entities entering, staying inside, and leaving the zone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[EntityCategory("Zones")]
public sealed partial class ZoneComponent : Component
{
    ///// <summary>
    ///// The entity that this zone is assigned to.
    ///// Used to determine local coordinates
    ///// </summary>
    //public EntityUid Parent = EntityUid.Invalid;

    ///// <summary>
    ///// Name of the zone
    ///// </summary>
    //[DataField, ViewVariables]
    //public string Name = string.Empty;

    /// <summary>
    /// ID of the zone's entity prototype
    /// </summary>
    //[DataField, ViewVariables(VVAccess.ReadOnly)]
    //public EntProtoId<ZoneComponent> ProtoID = DefaultZoneId;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.Gray;

    /// <summary>
    /// Should the size of the zone be attached to the grid
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AttachToLattice;

    [DataField, AutoNetworkedField]
    public ZoneSpaceOption SpaceOption = ZoneSpaceOption.None;

    /// <summary>
    /// Original size of the zone
    /// </summary>
    [DataField(readOnly: true), AutoNetworkedField]
    public List<Box2> Area = [];

    /// <summary>
    /// Disabled zone size
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2> DisabledArea = [];

    /// <summary>
    /// The <see cref="Area"/> with the cut-out <see cref="DisabledRegion"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2> ActiveArea = [];

    /// <summary>
    /// An array of entities currently located in the zone
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> EnteredEntities = [];
}

public enum ZoneSpaceOption
{
    None,
    Disable,
    Cut
}

public enum ZoneRegionType
{
    Original,
    Active,
    Disabled
}

[Serializable, NetSerializable]
public sealed class ZoneComponentState(ZoneParamsState state) : IComponentState
{
    public readonly ZoneParamsState State = state;
}
