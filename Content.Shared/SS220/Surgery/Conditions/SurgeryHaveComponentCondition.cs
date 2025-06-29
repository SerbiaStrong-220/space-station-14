// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using JetBrains.Annotations;

namespace Content.Shared.SS220.Surgery.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class SurgeryHaveComponentCondition : ISurgeryGraphCondition
{
    [DataField(required: true)]
    public string Component = "";

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        var compReg = entityManager.ComponentFactory.GetRegistration(Component);
        if (entityManager.HasComponent(uid, compReg.Type))
            return true;

        return false;
    }

    public void DoScanExamine()
    {

    }
}
