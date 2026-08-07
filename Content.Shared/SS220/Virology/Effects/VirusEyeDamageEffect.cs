// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusEyeDamageEffect : IVirusEffect
{
    /// <summary>Time in this stage to reach max "blind" effect.</summary>
    [DataField]
    public TimeSpan TimeToFull = TimeSpan.FromMinutes(5);

    public void ApplyEffect(in VirusProgressArgs args)
    {
        if (!args.EntityManager.TryGetComponent<BlindableComponent>(args.Carrier, out var blindable))
            return;

        // <= 0 would divide by zero
        var fraction = TimeToFull <= TimeSpan.Zero
            ? 1d
            : Math.Clamp((args.CurTime - args.Symptom.StageStartTime) / TimeToFull, 0d, 1d);
        var target = (int)(blindable.MaxDamage * fraction);

        if (target > blindable.EyeDamage)
            args.EntityManager.System<BlindableSystem>().AdjustEyeDamage((args.Carrier, blindable), target - blindable.EyeDamage);
    }
}
