// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

[ImplicitDataDefinitionForInheritors]
public abstract partial class PathologyProgressCondition
{
    [DataField]
    public bool Invert = false;

    protected abstract bool Condition(EntityUid uid, PathologyInstanceData instanceData, IEntityManager entityManager);

    public bool CheckCondition(EntityUid uid, PathologyInstanceData instanceData, IEntityManager entityManager)
    {
        return Invert != Condition(uid, instanceData, entityManager);
    }
}
