// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Conditions;

[UsedImplicitly]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class SurgeryToolTypeCondition : ISurgeryGraphCondition
{
    [DataField(required: true)]
    public SurgeryToolType SurgeryTool = SurgeryToolType.Invalid;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<SurgeryToolComponent>(uid, out var surgeryTool)
            || surgeryTool.ToolType != SurgeryTool)
            return false;

        return true;
    }

    public void DoScanExamine()
    {

    }
}
