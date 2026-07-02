// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyStaminaDamageEffect : IPathologyEffect
{
    /// <summary>Stamina drained per update while standing.</summary>
    [DataField]
    public float Amount = 5f;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        if (PathologyEffectConditions.IsRecumbent(args.Target, args.EntityManager))
            return;

        args.EntityManager.System<SharedStaminaSystem>().TryTakeStamina(args.Target, Amount);
    }
}
