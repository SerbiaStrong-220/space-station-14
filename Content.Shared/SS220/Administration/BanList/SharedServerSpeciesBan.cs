// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.BanList;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration.BanList;

[Serializable, NetSerializable]
public sealed record SharedServerSpeciesBan(
    int? Id,
    NetUserId? UserId,
    (string address, int cidrMask)? Address,
    string? HWId,
    DateTime BanTime,
    DateTime? ExpirationTime,
    string Reason,
    string? BanningAdminName,
    SharedServerUnban? Unban,
    string SpeciesId
) : SharedServerBan(Id, UserId, Address, HWId, BanTime, ExpirationTime, Reason, BanningAdminName, Unban);
