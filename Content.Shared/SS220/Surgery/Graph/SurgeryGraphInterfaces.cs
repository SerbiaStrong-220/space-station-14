// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
public partial interface ISurgeryGraphCondition
{
    bool Condition(EntityUid uid, IEntityManager entityManager);
    void DoScanExamine(); // surgery_TODO: make it seen in med scanner
}

[ImplicitDataDefinitionForInheritors]
public partial interface ISurgeryGraphAction
{
    void PerformAction(EntityUid uid, EntityUid? userUid, EntityUid? used, IEntityManager entityManager);
}

