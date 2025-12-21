// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Database;
using Robust.Shared.Network;
using System.Net;

namespace Content.Server.SS220.Database;
public sealed class ServerSpeciesBanDef
{
    public int? Id { get; }
    public NetUserId? UserId { get; }
    public (IPAddress address, int cidrMask)? Address { get; }
    public ImmutableTypedHwid? HWId { get; }

    public DateTimeOffset BanTime { get; }
    public DateTimeOffset? ExpirationTime { get; }
    public int? RoundId { get; }
    public TimeSpan PlaytimeAtNote { get; }
    public string Reason { get; }
    public NoteSeverity Severity { get; set; }
    public NetUserId? BanningAdmin { get; }
    public ServerSpeciesUnbanDef? Unban { get; }
    public string SpeciesId { get; }

    public ServerSpeciesBanDef(
        int? id,
        NetUserId? userId,
        (IPAddress, int)? address,
        ImmutableTypedHwid? hwId,
        DateTimeOffset banTime,
        DateTimeOffset? expirationTime,
        int? roundId,
        TimeSpan playtimeAtNote,
        string reason,
        NoteSeverity severity,
        NetUserId? banningAdmin,
        ServerSpeciesUnbanDef? unban,
        string speciesId)
    {
        if (userId == null && address == null && hwId == null)
        {
            throw new ArgumentException("Must have at least one of banned user, banned address or hardware ID");
        }

        if (address is { } addr && addr.Item1.IsIPv4MappedToIPv6)
        {
            // Fix IPv6-mapped IPv4 addresses
            // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
            address = (addr.Item1.MapToIPv4(), addr.Item2 - 96);
        }

        Id = id;
        UserId = userId;
        Address = address;
        HWId = hwId;
        BanTime = banTime;
        ExpirationTime = expirationTime;
        RoundId = roundId;
        PlaytimeAtNote = playtimeAtNote;
        Reason = reason;
        Severity = severity;
        BanningAdmin = banningAdmin;
        Unban = unban;
        SpeciesId = speciesId;
    }
}
