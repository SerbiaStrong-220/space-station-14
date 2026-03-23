// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class BrainDamageTimerChange : EntityEffectBase<BrainDamageTimerChange>
{
    /// <summary>
    /// How long will brain damage be delayed with one assimilation of the reagent?
    /// </summary>
    [DataField(required: true)]
    public TimeSpan AddTime;

    /// <summary>
    ///     Prototype of the reagent we're adding.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-ss220-brain-damage-slow", ("time", AddTime.TotalSeconds));

}
