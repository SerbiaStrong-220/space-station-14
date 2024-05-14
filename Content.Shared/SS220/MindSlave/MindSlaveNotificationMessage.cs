// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindSlave;

[Serializable, NetSerializable]
public sealed class MindSlaveNotificationEuiState : EuiStateBase
{
    public readonly string MasterName;
    public readonly bool IsEnslaved;

    public MindSlaveNotificationEuiState(string masterName, bool isEnslaved)
    {
        MasterName = masterName;
        IsEnslaved = isEnslaved;
    }
}
