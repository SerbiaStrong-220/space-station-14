// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.MartialArts.Sequence.Conditions;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CombatSequenceCondition
{
    [DataField]
    public bool Invert = false;

    private IEntityManager? _entMan;
    private IGameTiming? _timing;

    protected IEntityManager Entity => _entMan ??= IoCManager.Resolve<IEntityManager>();
    protected IGameTiming Timing => _timing ??= IoCManager.Resolve<IGameTiming>();

    public virtual bool Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        return true;
    }
}
