using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Zones.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ZonesDataComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Dictionary<int, ZoneData> Zones = new();

    public int GetFreeZoneId()
    {
        var i = 1;
        while (Zones.ContainsKey(i))
            i++;

        return i;
    }
}

[Serializable, NetSerializable]
public sealed class ZoneData()
{
    [ViewVariables]
    public string Name = string.Empty;

    [ViewVariables]
    public string EntityId = "BaseZone";

    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity? ZoneEntity;

    [ViewVariables]
    public Color Color = Color.Red;

    [ViewVariables]
    public HashSet<Vector2i> Tiles = new();
}
