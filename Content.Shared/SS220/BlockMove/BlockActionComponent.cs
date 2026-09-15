using Robust.Shared.GameStates;

namespace Content.Shared.SS220.BlockMove;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class BlockActionComponent : Component
{
    [DataField]
    public float? Duration;

    [DataField]
    public bool BlockShoot = true;

    [DataField]
    public bool BlockMove = true;

    [DataField]
    public bool BlockAttack = true;

    [DataField]
    public string BlockMoveEffectProto = "BlockMoveEffect";
}
