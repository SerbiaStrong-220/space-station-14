// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Database;
using Content.Shared.Database;

namespace Content.Server.SS220.Database;

public sealed record ServerSpeciesBanNoteRecord(
    int Id,
    RoundRecord? Round,
    PlayerRecord? Player,
    TimeSpan PlaytimeAtNote,
    string Message,
    NoteSeverity Severity,
    PlayerRecord? CreatedBy,
    DateTimeOffset CreatedAt,
    PlayerRecord? LastEditedBy,
    DateTimeOffset? LastEditedAt,
    DateTimeOffset? ExpirationTime,
    bool Deleted,
    string[] Species,
    PlayerRecord? UnbanningAdmin,
    DateTime? UnbanTime) : IAdminRemarksRecord;
