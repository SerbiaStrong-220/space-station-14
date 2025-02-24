using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PenScrambler;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SetScaleFromTargetComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public NetEntity? Target;

    [DataField]
    public bool IsUpdated;
}

[Serializable, NetSerializable]
public sealed class SetScaleFromTargetEvent : EntityEventArgs
{
    public NetEntity Owner { get; }
    public NetEntity? Target { get; }

    public SetScaleFromTargetEvent(NetEntity owner, NetEntity? target)
    {
        Owner = owner;
        Target = target;
    }
}
