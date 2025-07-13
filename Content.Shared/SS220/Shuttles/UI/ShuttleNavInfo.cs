
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.UI;

[Serializable, NetSerializable]
public abstract class ShuttleNavInfo { }

#region Projectile
[Serializable, NetSerializable]
public sealed class ShuttleNavProjectilesInfo : ShuttleNavInfo
{
    public List<ProjectileInfo> Infos = [];

    [Serializable, NetSerializable]
    public struct ProjectileInfo
    {
        public MapCoordinates Coordinates;
        public float CircleRadius;
        public Color Color;
    }
}
#endregion
