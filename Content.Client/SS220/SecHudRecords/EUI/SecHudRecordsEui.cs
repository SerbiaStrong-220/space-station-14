using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.SecHudRecords;
using JetBrains.Annotations;

namespace Content.Client.SS220.SecHudRecords.EUI;

[UsedImplicitly]
public sealed class SecHudRecordsEui : BaseEui
{
    private readonly SecHudRecordsMenu _menu;

    public SecHudRecordsEui()
    {
        _menu = new SecHudRecordsMenu();
        _menu.OnClose += OnClosed;
    }

    private void OnClosed()
    {
        SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        base.Opened();
        _menu.OpenCenteredRight();
    }

    public override void Closed()
    {
        base.Closed();
        _menu.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        var secHudState = (SecHudRecordsEuiState) state;
        _menu.TargetEntityId = secHudState.TargetNetEntity;
        _menu.FullCatalog = secHudState.FullCatalog;
        _menu.GeneralRecord = secHudState.Record;
        _menu.PopulatePrototypes();
    }
}
