// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Players;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Discord;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.Discord;

public sealed class DiscordPlayerManager : IPostInjectInit, IDisposable
{
    internal SponsorUsers? CachedSponsorUsers => _cachedSponsorUsers;

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;
    private Timer? _statusRefreshTimer; // We should keep reference or else evil GC will kill our timer
    private volatile SponsorUsers? _cachedSponsorUsers;
    private readonly HttpClient _httpClient = new();

    private string _linkApiUrl = string.Empty;
    private bool _isDiscordLinkRequired = false;

    public event EventHandler<ICommonSession>? PlayerVerified;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");

        _netMgr.RegisterNetMessage<MsgUpdatePlayerDiscordStatus>();

        _netMgr.RegisterNetMessage<MsgDiscordLinkRequired>();
        _netMgr.RegisterNetMessage<MsgRecheckDiscordLink>(CheckDiscordLinked);
        _netMgr.RegisterNetMessage<MsgByPassDiscordCheck>(ByPassDiscordCheck);

        _cfg.OnValueChanged(CCVars220.DiscordLinkApiUrl, v => _linkApiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.DiscordLinkRequired, v => _isDiscordLinkRequired = v, true);
        _cfg.OnValueChanged(CCVars220.DiscordLinkApiKey, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v);
        },
        true);

        _statusRefreshTimer = new Timer(async _ =>
            {
                _cachedSponsorUsers = await GetSponsorUsers();
            },
            state: null,
            dueTime: TimeSpan.FromSeconds(_cfg.GetCVar(CCVars220.DiscordSponsorsCacheLoadDelaySeconds)),
            period: TimeSpan.FromSeconds(_cfg.GetCVar(CCVars220.DiscordSponsorsCacheRefreshIntervalSeconds))
        );
    }

    private void ByPassDiscordCheck(MsgByPassDiscordCheck msg)
    {
        var session = _playerManager.GetSessionById(msg.MsgChannel.UserId);

        PlayerVerified?.Invoke(this, session);
    }

    void IPostInjectInit.PostInject()
    {
        _playerManager.PlayerStatusChanged += PlayerManager_PlayerStatusChanged;
    }

    private async void CheckDiscordLinked(MsgRecheckDiscordLink msg)
    {
        var isLinked = await CheckUserLink(msg.MsgChannel.UserId);

        if (isLinked)
        {
            var session = _playerManager.GetSessionById(msg.MsgChannel.UserId);

            PlayerVerified?.Invoke(this, session);
        }
    }

    public void Dispose()
    {
        _statusRefreshTimer?.Dispose();
        _httpClient.Dispose();
    }

    private async void PlayerManager_PlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            if (!_isDiscordLinkRequired)
            {
                PlayerVerified?.Invoke(this, e.Session);
                return;
            }

            var isLinked = await CheckUserLink(e.Session.UserId);

            if (isLinked)
            {
                PlayerVerified?.Invoke(this, e.Session);
                return;
            }

            var url = await GetUserLink(e.Session.UserId);

            var msg = new MsgDiscordLinkRequired() { AuthUrl = url };

            e.Session.Channel.SendMessage(msg);
        }

        if (e.NewStatus == SessionStatus.InGame)
        {
            await UpdateUserDiscordRolesStatus(e);
        }
    }

    private async Task UpdateUserDiscordRolesStatus(SessionStatusEventArgs e)
    {
        var info = await GetSponsorInfo(e.Session.UserId);

        if (info is not null)
        {
            _netMgr.ServerSendMessage(new MsgUpdatePlayerDiscordStatus
            {
                Info = info
            },
            e.Session.Channel);

            // Cache info in content data
            var contentPlayerData = e.Session.ContentData();
            if (contentPlayerData == null)
                return;

            contentPlayerData.SponsorInfo = info;
        }
    }

    private async Task<DiscordSponsorInfo?> GetSponsorInfo(NetUserId userId)
    {
        if (string.IsNullOrEmpty(_linkApiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_linkApiUrl}/api/userinfo/{WebUtility.UrlEncode(userId.ToString())}";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get player sponsor info: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<DiscordSponsorInfo>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }

    public async Task<string> GetUserLink(NetUserId userId)
    {
        try
        {
            _sawmill.Debug($"Player {userId} get Discord link");

            var requestUrl = $"{_linkApiUrl}/api/linkAccount/link14/{WebUtility.UrlEncode(userId.ToString())}";
            var response = await _httpClient.PostAsync(requestUrl, content: null, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                throw new Exception($"Failed to get user discord link. API returned bad status code: {response.StatusCode}\nResponse: {content}");
            }

            var data = await response.Content.ReadFromJsonAsync<AccountLinkResponseParameters>();
            return data!.AccountLinkUrl;
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Exception on user link get. {exc}");
        }

        return string.Empty;
    }

    public async Task<bool> CheckUserLink(NetUserId userId)
    {
        try
        {
            _sawmill.Debug($"Player {userId} check Discord link");

            var requestUrl = $"{_linkApiUrl}/api/linkAccount/checkLink14/{WebUtility.UrlEncode(userId.ToString())}";

            var response = await _httpClient.GetAsync(requestUrl, CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                throw new Exception($"Failed to check user discord link. API returned bad status code: {response.StatusCode}\nResponse: {content}");
            }

            var data = await response.Content.ReadFromJsonAsync<DiscordAuthInfoResponse>();

            return data!.AccountLinked;
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Exception on check user link. {exc}");
        }

        return false;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var opt = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        opt.Converters.Add(new JsonStringEnumConverter());

        return opt;
    }

    public async Task<PrimeListUserStatus?> GetUserPrimeListStatus(Guid userId)
    {
        if (string.IsNullOrEmpty(_linkApiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_linkApiUrl}/api/checkPrimeAccess/{userId}";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get user prime list status: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<PrimeListUserStatus>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }

    /// <summary>
    /// Возвращает список спонсоров проекта.
    /// </summary>
    /// <returns></returns>
    internal async Task<SponsorUsers?> GetSponsorUsers()
    {
        if (string.IsNullOrWhiteSpace(_linkApiUrl))
        {
            return null;
        }

        try
        {
            var url = $"{_linkApiUrl}/api/userinfo/sponsors";
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to get sponsor users info: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);

                return null;
            }

            return await response.Content.ReadFromJsonAsync<SponsorUsers>(GetJsonSerializerOptions());
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }

        return null;
    }

    private sealed record AccountLinkResponseParameters(string AccountLinkUrl);

    private sealed record DiscordAuthInfoResponse(bool AccountLinked);
}
