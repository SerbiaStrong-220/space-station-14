
using System.Numerics;

namespace Content.Shared.SS220.GridZones.Components;

public sealed partial class GridZonesComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<NetEntity, HashSet<Vector2>> Zones = new();
}
