// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

// currently executes server-side only, probably implementation should be separated from declaration
// but currently this is not a problem
[ImplicitDataDefinitionForInheritors]
public abstract partial class CombatSequenceEffect
{
    private IEntityManager? _entMan;
    private IGameTiming? _timing;

    protected IEntityManager Entity => _entMan ??= IoCManager.Resolve<IEntityManager>();
    protected IGameTiming Timing => _timing ??= IoCManager.Resolve<IGameTiming>();

    public abstract void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist);
}
