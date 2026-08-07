// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusStaminaDamageEffect : IVirusEffect
{
    /// <summary>Stamina drained per update while standing/running.</summary>
    [DataField]
    public float Amount = 2f;

    public void ApplyEffect(in VirusProgressArgs args)
    {
        if (VirusEffectConditions.IsRecumbent(args.Carrier, args.EntityManager))
            return;

        args.EntityManager.System<SharedStaminaSystem>().TryTakeStamina(args.Carrier, Amount);
    }
}
