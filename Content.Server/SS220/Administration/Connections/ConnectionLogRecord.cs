// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
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

