using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;


namespace Content.Server.Database
{
    public sealed class BanDef
    {
        public int? Id { get; }
        public BanType Type { get; }
        public ImmutableArray<NetUserId> UserIds { get; }
        public ImmutableArray<(IPAddress address, int cidrMask)> Addresses { get; }
        public ImmutableArray<ImmutableTypedHwid> HWIds { get; }

        public DateTimeOffset BanTime { get; }
        public DateTimeOffset? ExpirationTime { get; }
        public ImmutableArray<int> RoundIds { get; }
        public TimeSpan PlaytimeAtNote { get; }
        public string Reason { get; }
        public NoteSeverity Severity { get; set; }
        public NetUserId? BanningAdmin { get; }
        public string? BanningAdminName { get; } // SS220-save-admin-name
        public UnbanDef? Unban { get; }
        public ServerBanExemptFlags ExemptFlags { get; }

        public ImmutableArray<IBanRoleDef>? Roles { get; } // SS220-ase-abstract-gush

        public BanDef(
            int? id,
            BanType type,
            ImmutableArray<NetUserId> userIds,
            ImmutableArray<(IPAddress address, int cidrMask)> addresses,
            ImmutableArray<ImmutableTypedHwid> hwIds,
            DateTimeOffset banTime,
            DateTimeOffset? expirationTime,
            ImmutableArray<int> roundIds,
            TimeSpan playtimeAtNote,
            string reason,
            NoteSeverity severity,
            NetUserId? banningAdmin,
            string? banningAdminName, // SS220-save-admin-name
            UnbanDef? unban,
            ServerBanExemptFlags exemptFlags = default,
            ImmutableArray<IBanRoleDef>? roles = null) // SS220-ase-abstract-gush
        {
            if (userIds.Length == 0 && addresses.Length == 0 && hwIds.Length == 0)
            {
                throw new ArgumentException("Must have at least one of banned user, banned address or hardware ID");
            }

            addresses = addresses.Select(address =>
                {
                    if (address is { address.IsIPv4MappedToIPv6: true } addr)
                    {
                        // Fix IPv6-mapped IPv4 addresses
                        // So that IPv4 addresses are consistent between separate-socket and dual-stack socket modes.
                        address = (addr.address.MapToIPv4(), addr.cidrMask - 96);
                    }

                    return address;
                })
                .ToImmutableArray();

            Id = id;
            Type = type;
            UserIds = userIds;
            Addresses = addresses;
            HWIds = hwIds;
            BanTime = banTime;
            ExpirationTime = expirationTime;
            RoundIds = roundIds;
            PlaytimeAtNote = playtimeAtNote;
            Reason = reason;
            Severity = severity;
            BanningAdmin = banningAdmin;
            BanningAdminName = banningAdminName; // SS220-save-admin-name
            Unban = unban;
            ExemptFlags = exemptFlags;

            switch (Type)
            {
                case BanType.Server:
                    if (roles != null)
                        throw new ArgumentException("Cannot specify roles for server ban types", nameof(roles));
                    break;

                // SS220-add-bans-for-spices-chats-begin
                case BanType.Species:
                case BanType.Chat:
                // SS220-add-bans-for-spices-chats-end
                case BanType.Role:
                    if (roles is not { Length: > 0 })
                        throw new ArgumentException("Must specify roles for server ban types", nameof(roles));
                    if (exemptFlags != 0)
                        throw new ArgumentException("Role bans cannot have exempt flags", nameof(exemptFlags));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            Roles = roles;
        }

        public string FormatBanMessage(IConfigurationManager cfg, ILocalizationManager loc, string? playerLogin) // SS220-ad-login-into-ban-screen
        {
            string expires;
            if (ExpirationTime is { } expireTime)
            {
                var duration = expireTime - BanTime;
                var utc = expireTime.ToUniversalTime();
                expires = loc.GetString("ban-expires", ("duration", duration.TotalMinutes.ToString("N0")), ("time", utc.ToString("f")));
            }
            else
            {
                var appeal = cfg.GetCVar(CCVars.InfoLinksAppeal);
                expires = !string.IsNullOrWhiteSpace(appeal)
                    ? loc.GetString("ban-banned-permanent-appeal", ("link", appeal))
                    : loc.GetString("ban-banned-permanent");
            }

            playerLogin = playerLogin is null ? "" : playerLogin; // SS220-ad-login-into-ban-screen
            string additionalInfo = cfg.GetCVar(CCVars220.AdditionalBanInfo); // add-some-admin-changeable-info

            return $"""
                   {loc.GetString("ban-banned-1")}
                   {loc.GetString("ban-banned-8", ("banId", Id.HasValue ? Id.Value : "-"))}
                   {loc.GetString("ban-banned-4", ("admin", BanningAdminName ?? "Console"))}
                   {loc.GetString("ban-banned-9", ("login", playerLogin))}
                   {loc.GetString("ban-banned-6", ("round", RoundIds.Length != 0 ? RoundIds[0] : loc.GetString("ban-banned-7")))}
                   {loc.GetString("ban-banned-2", ("reason", Reason))}
                   {expires}
                   {loc.GetString("ban-banned-3")}
                   {additionalInfo}
                   """;
        }
    }
}
