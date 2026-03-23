using System.Net;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class BanPanelEuiState : EuiStateBase
{
    public string PlayerName { get; set; }
    public bool HasBan { get; set; }

    public BanPanelEuiState(string playerName, bool hasBan)
    {
        PlayerName = playerName;
        HasBan = hasBan;
    }
}

public static class BanPanelEuiStateMsg
{
    [Serializable, NetSerializable]
    public sealed class CreateBanRequest(Ban ban) : EuiMessageBase
    {
        public Ban Ban { get; } = ban;
    }

    [Serializable, NetSerializable]
    public sealed class GetPlayerInfoRequest : EuiMessageBase
    {
        public string PlayerUsername { get; set; }

        public GetPlayerInfoRequest(string username)
        {
            PlayerUsername = username;
        }
    }
}

/// <summary>
///     Contains all the data related to a particular ban action created by the BanPanel window.
/// </summary>
[Serializable, NetSerializable]
public sealed record Ban
{
    public Ban(
        string? target,
        (IPAddress, int)? ipAddressTuple,
        bool useLastIp,
        ImmutableTypedHwid? hwid,
        bool useLastHwid,
        uint banDurationMinutes,
        string reason,
        NoteSeverity severity,
        int statedRound,
        ProtoId<JobPrototype>[]? bannedJobs,
        ProtoId<AntagPrototype>[]? bannedAntags,
        ProtoId<SpeciesPrototype>[]? bannedSpecies, // SS220-species-ban
        bool erase,
        bool postBanInfo) // SS220 Post ban info option
    {
        Target = target;
        IpAddress = ipAddressTuple?.Item1.ToString();
        IpAddressHid = ipAddressTuple?.Item2.ToString() ?? "0";
        UseLastIp = useLastIp;
        Hwid = hwid;
        UseLastHwid = useLastHwid;
        BanDurationMinutes = banDurationMinutes;
        Reason = reason;
        Severity = severity;
        StatedRound = statedRound;
        BannedJobs = bannedJobs;
        BannedAntags = bannedAntags;
        BannedSpecies = bannedSpecies; // SS220-species-ban
        Erase = erase;
        PostBanInfo = postBanInfo; // SS220 Post ban info option
    }

    public readonly string? Target;
    public readonly string? IpAddress;
    public readonly string? IpAddressHid;
    public readonly bool UseLastIp;
    public readonly ImmutableTypedHwid? Hwid;
    public readonly bool UseLastHwid;
    public readonly uint BanDurationMinutes;
    public readonly string Reason;
    public readonly NoteSeverity Severity;
    public readonly int StatedRound;
    public readonly ProtoId<JobPrototype>[]? BannedJobs;
    public readonly ProtoId<AntagPrototype>[]? BannedAntags;
    public readonly ProtoId<SpeciesPrototype>[]? BannedSpecies;
    public readonly bool Erase;
    public readonly bool PostBanInfo;
}
