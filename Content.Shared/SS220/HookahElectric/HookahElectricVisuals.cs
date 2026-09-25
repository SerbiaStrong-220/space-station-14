using Robust.Shared.Serialization;

namespace Content.Shared.SS220.HookahElectric;

[Serializable, NetSerializable]
public enum HookahElectricVisuals : byte
{
    Enabled,
    LeftHose,
    RightHose,
}

[Serializable, NetSerializable]
public enum HookahElectricVisualLayers : byte
{
    Base,
    LeftHose,
    RightHose,
}
