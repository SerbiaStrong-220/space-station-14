using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.SS220.Headphones;

/// <summary>
/// ...
/// </summary>
[RegisterComponent]
public sealed partial class HeadphonesComponent : Component
{
    [DataField]
    public float VolumeModificator = 1f;
}

