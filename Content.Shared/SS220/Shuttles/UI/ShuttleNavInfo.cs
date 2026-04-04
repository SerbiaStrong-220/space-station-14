// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.UI;

[Serializable, NetSerializable, DataDefinition]
public abstract partial class ShuttleNavInfo
{
    [DataField]
    public bool Enabled = false;
}

/// <summary>
/// Information for drawing projectile as circle on a nav map
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShuttleNavProjectileInfo : ShuttleNavInfo
{
    /// <summary>
    /// Circle color
    /// </summary>
    [DataField]
    public Color Color = Color.Yellow;

    /// <summary>
    /// Circle radius
    /// </summary>
    [DataField]
    public float Radius = 0.75f;
}

/// <summary>
/// Information for drawing hitscan as line on a nav map
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShuttleNavHitscanInfo : ShuttleNavInfo
{
    /// <summary>
    /// Line color
    /// </summary>
    [DataField]
    public Color Color = Color.Red;

    /// <summary>
    /// Line width
    /// </summary>
    [DataField]
    public float Width = 1f;

    /// <summary>
    /// How long will the hitscan take to draw after the shot
    /// </summary>
    [DataField]
    public TimeSpan AnimationLength = TimeSpan.FromSeconds(1f);
}

/// <summary>
/// Information for drawing forcefield on nav map
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShuttleNavForcefieldInfo : ShuttleNavInfo
{
    /// <summary>
    /// Fircefield color
    /// </summary>
    public Color Color = Color.LightBlue;

    public List<MapCoordinates> TrianglesVerts = new();
}
