using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Clothing;

/// <summary>
///     Generic enum keys for toggle-visualizer appearance data & sprite layers.
/// </summary>
[Serializable, NetSerializable]
public enum InnerHandToggleableVisuals : byte
{
    Toggled,
    Layer
}
