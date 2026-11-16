// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Standing;

namespace Content.Shared.SS220.MartialArts.Sequence.Conditions;

public sealed partial class IsDownCombatCondition : CombatSequenceCondition
{
    public override bool Execute(Entity<MartialArtistComponent> user, EntityUid target)
    {
        var standing = Entity.System<StandingStateSystem>();
        return standing.IsDown(target);
    }
}
