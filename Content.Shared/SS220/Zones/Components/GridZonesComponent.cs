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
    public string Name = string.Empty;

    public string? EntityId;

    public NetEntity? ZoneEntity;

    public Color Color = Color.Gray;

    public HashSet<Vector2i> Tiles = new();
}
