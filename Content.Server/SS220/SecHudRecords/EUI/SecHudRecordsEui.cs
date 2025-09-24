using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.SS220.SecHudRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SecHudRecords.EUI;

public sealed class SecHudRecordsEui(NetEntity target, List<(ProtoId<CriminalStatusPrototype>?, string)> fullCatalog) : BaseEui
{
    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SecHudRecordsEuiState
        {
            TargetNetEntity = target,
            FullCatalog = fullCatalog,
        };
    }
}
