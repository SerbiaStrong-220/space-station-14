// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ModifyMovementSpeedOnAdapt : EntityEffectBase<ModifyMovementSpeedOnAdapt>
{
    /// <summary>
    /// How much the entities' walk speed is multiplied by.(is 1f by default, will increase/decrease on adaptation)
    /// </summary>
    [DataField]
    public float WalkSpeedModifier = 1f;

    /// <summary>
    /// How much the entities' run speed is multiplied by.
    /// </summary>
    [DataField]
    public float SprintSpeedModifier = 1f;

}
