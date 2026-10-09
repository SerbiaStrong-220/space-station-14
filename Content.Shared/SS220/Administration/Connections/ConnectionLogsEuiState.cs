using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration.Connections;

[Serializable, NetSerializable]
public sealed class ConnectionLogInfo(
    int id,
    string userName,
    DateTime time,
    string address,
    string? deniedReason,
    string? serverName,
    float trust)
{
    public int Id { get; } = id;
    public string UserName { get; } = userName;
    public DateTime Time { get; } = time;
    public string Address { get; } = address;
    public string? DeniedReason { get; } = deniedReason;
    public string? ServerName { get; } = serverName;
    public float Trust { get; } = trust;
}

[Serializable, NetSerializable]
public sealed class ConnectionLogsEuiState(
    string ckey,
    List<ConnectionLogInfo> logs,
    bool isLoading,
    bool hasNext,
    string? error)
    : EuiStateBase
{
    public string Ckey { get; } = ckey;
    public List<ConnectionLogInfo> Logs { get; } = logs;
    public bool IsLoading { get; } = isLoading;
    public bool HasNext { get; } = hasNext;
    public string? Error { get; } = error;
}

public static class ConnectionLogsEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Search(string ckey) : EuiMessageBase
    {
        public string Ckey { get; } = ckey;
    }

    [Serializable, NetSerializable]
    public sealed class NextPage : EuiMessageBase;
}

[Serializable, NetSerializable]
public sealed class PlayerPanelConnectionLogsMessage : EuiMessageBase;
