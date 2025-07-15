// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.UI;

[Serializable, NetSerializable, DataDefinition]
public abstract partial class ShuttleNavInfo
{
    [DataField]
    public bool Enabled = false;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShuttleNavProjectileInfo : ShuttleNavInfo
{
    [DataField]
    public Color Color = Color.Yellow;

    [DataField]
    public float Radius = 0.75f;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShuttleNavHitscanInfo : ShuttleNavInfo
{
    [DataField]
    public Color Color = Color.Red;

    [DataField]
    public float Width = 0.5f;

    [DataField]
    public TimeSpan AnimationLength = TimeSpan.FromSeconds(1f);
}
