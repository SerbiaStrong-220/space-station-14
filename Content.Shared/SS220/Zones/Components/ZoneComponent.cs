// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Zones.Components;

/// <summary>
/// The component used to determine the zones located on the <see cref="Container"/>.
/// A zone can be used to determine a certain area on the <see cref="Container"/>
/// in which various events can occur, as well as with entities entering, staying inside, and leaving the zone.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZonesSystem))]
public sealed partial class ZoneComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public ZoneParams? ZoneParams;

    /// <summary>
    /// An array of entities located in the zone
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Entities = new();
}

[Serializable, NetSerializable]
public sealed partial class ZoneParams
{
    /// <summary>
    /// The entity that this zone is assigned to.
    /// Used to determine local coordinates
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity Container;

    [ViewVariables]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string ProtoId = SharedZonesSystem.BaseZoneId;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    [ViewVariables]
    public Color Color = SharedZonesSystem.DefaultColor;

    [ViewVariables]
    public bool AttachToGrid = false;

    /// <summary>
    /// Boxes in local coordinates (attached to <see cref="Container"/>) that determine the size of the zone
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Box2> Boxes = new();

    public void HandleState(ZoneParamsState @params)
    {
        Container = @params.Container;
        Name = @params.Name;
        ProtoId = @params.ProtoId;
        Color = @params.Color;
        AttachToGrid = @params.AttachToGrid;
        Boxes = @params.Boxes;
    }

    public ZoneParamsState GetState()
    {
        return new ZoneParamsState()
        {
            Container = Container,
            Name = Name,
            ProtoId = ProtoId,
            Color = Color,
            AttachToGrid = AttachToGrid,
            Boxes = Boxes
        };
    }
}
