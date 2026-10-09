using Content.Server.Database;

namespace Content.Server.SS220.Administration.Connections;

public sealed record ConnectionLogRecord(
    int Id,
    string UserName,
    DateTime Time,
    string Address,
    ConnectionDenyReason? Denied,
    string? ServerName,
    float Trust);

