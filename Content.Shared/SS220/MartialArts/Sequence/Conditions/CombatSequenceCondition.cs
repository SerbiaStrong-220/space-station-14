// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.MartialArts.Sequence.Conditions;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CombatSequenceCondition
{
    [DataField]
    public bool Invert = false;

    protected IEntityManager Entity => IoCManager.Resolve<IEntityManager>();

    public virtual bool Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        return true;
    }
}
