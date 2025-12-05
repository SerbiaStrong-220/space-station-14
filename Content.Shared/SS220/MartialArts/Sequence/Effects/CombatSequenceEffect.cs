// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

// currently executes server-side only, probably implementation should be separated from declaration
// but currently this is not a problem
[ImplicitDataDefinitionForInheritors]
public abstract partial class CombatSequenceEffect
{
    protected IEntityManager Entity => IoCManager.Resolve<IEntityManager>();

    public abstract void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist);
}
