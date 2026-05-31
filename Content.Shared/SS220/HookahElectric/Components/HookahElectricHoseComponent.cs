using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.HookahElectric.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HookahElectricHoseComponent : Component
{
    public EntityUid HookahUid;

    [DataField]
    public HookahElectricHoseSide Side = HookahElectricHoseSide.Left;

    [DataField]
    public float MaxDistance = 3f;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(0.25);
}

[Serializable, NetSerializable]
public enum HookahElectricHoseSide : byte
{
    Left,
    Right,
}
