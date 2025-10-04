using Content.Shared.EntityEffects;
using Content.Shared.SS220.BlockMove;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects;

public sealed partial class BlockActionEffect : EntityEffect
{
    [DataField]
    public float Duration;

    [DataField]
    public bool BlockShoot = true;

    [DataField]
    public bool BlockMove = true;

    [DataField]
    public bool BlockAttack = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return string.Empty;
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.EnsureComponent<BlockActionComponent>(args.TargetEntity, out var blockMoveComponent);
        blockMoveComponent.Duration = Duration;
        blockMoveComponent.BlockMove = BlockMove;
        blockMoveComponent.BlockShoot = BlockShoot;
        blockMoveComponent.BlockAttack = BlockAttack;
    }
}
