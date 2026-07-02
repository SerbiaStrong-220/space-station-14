// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;


public sealed partial class PathologyInjectReagentEffect : IPathologyEffect
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    /// <summary>Amount injected per interval at normal metabolic speed (multiplier 1).</summary>
    [DataField]
    public FixedPoint2 Amount = FixedPoint2.New(1);

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        var amount = Amount;
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.Target, out var blood)
            && blood.UpdateIntervalMultiplier > 0f)
        {
            amount /= blood.UpdateIntervalMultiplier;
        }

        var solution = new Solution(Reagent, amount);
        args.EntityManager.System<SharedBloodstreamSystem>().TryAddToBloodstream(args.Target, solution);
    }
}
