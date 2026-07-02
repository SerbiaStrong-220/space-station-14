// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyEyeDamageEffect : IPathologyEffect
{
    /// <summary>Time in this stage to reach fully "blind" eyes.</summary>
    [DataField]
    public TimeSpan TimeToFull = TimeSpan.FromMinutes(5);

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BlindableComponent>(args.Target, out var blindable))
            return;

        // <= 0 would divide by zero
        var fraction = TimeToFull <= TimeSpan.Zero
            ? 1d
            : Math.Clamp((args.CurTime - args.Data.StageStartTime) / TimeToFull, 0d, 1d);
        var target = (int)(blindable.MaxDamage * fraction);

        if (target > blindable.EyeDamage)
            args.EntityManager.System<BlindableSystem>().AdjustEyeDamage((args.Target, blindable), target - blindable.EyeDamage);
    }
}
