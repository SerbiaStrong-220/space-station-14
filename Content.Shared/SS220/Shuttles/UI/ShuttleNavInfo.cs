
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.UI;

[Serializable, NetSerializable, DataDefinition]
public abstract partial class ShuttleNavInfo
{
    [DataField]
    public bool Show = false;
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
    public TimeSpan AnimationLength = TimeSpan.FromSeconds(1.5f);
}
