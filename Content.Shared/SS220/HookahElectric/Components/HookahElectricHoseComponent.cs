using Robust.Shared.Serialization;

namespace Content.Shared.SS220.HookahElectric.Components;

[RegisterComponent]
public sealed partial class HookahElectricHoseComponent : Component
{
    [DataField]
    public HookahElectricHoseSide Side = HookahElectricHoseSide.Left;
}

[Serializable, NetSerializable]
public enum HookahElectricHoseSide : byte
{
    Left,
    Right,
}
