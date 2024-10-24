// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SS220.MindSlave;

namespace Content.Server.SS220.MindSlave.UI;

public sealed class MindSlaveNotificationEui : BaseEui
{
    public readonly string MasterName = string.Empty;
    public readonly bool IsEnslaved;

    public MindSlaveNotificationEui(string masterName, bool isEnslaved)
    {
        MasterName = masterName;
        IsEnslaved = isEnslaved;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var state = new MindSlaveNotificationEuiState(MasterName, IsEnslaved);
        return state;
    }
}

