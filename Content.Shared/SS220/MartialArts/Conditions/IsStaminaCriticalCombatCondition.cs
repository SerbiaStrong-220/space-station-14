// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Components;

namespace Content.Shared.SS220.MartialArts.Conditions;

public sealed partial class IsStaminaCriticalCombatCondition : CombatSequenceCondition
{
    public override bool Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        if (!Entity.TryGetComponent<StaminaComponent>(target, out var stamina))
            return false;
        return stamina.Critical;
    }
}
