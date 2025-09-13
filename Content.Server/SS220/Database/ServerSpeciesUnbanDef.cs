// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Network;

namespace Content.Server.SS220.Database;

public sealed class ServerSpeciesUnbanDef(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
{
    public int BanId { get; } = banId;

    public NetUserId? UnbanningAdmin { get; } = unbanningAdmin;

    public DateTimeOffset UnbanTime { get; } = unbanTime;
}
