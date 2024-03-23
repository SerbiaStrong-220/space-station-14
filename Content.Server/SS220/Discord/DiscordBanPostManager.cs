// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Corvax.CCCVars;
using Robust.Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace Content.Server.SS220.Discord;

public sealed class DiscordBanPostManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ISawmill _sawmill = default!;

    private readonly HttpClient _httpClient = new();
    private string _apiUrl = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");

        // _netMgr.RegisterNetMessage<MsgUpdatePlayerDiscordStatus>();

        _cfg.OnValueChanged(CCCVars.DiscordAuthApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.DiscordAuthApiKey, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v);
        },
        true);
    }

    public async Task PostUserBanInfo(int banId)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            return;
        }

        try
        {
            var url = $"{_apiUrl}/userban/{banId}";

            var response = await _httpClient.PostAsync(url, content: null);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to post user ban: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);
            }
        }
        catch (Exception exc)
        {
            _sawmill.Error(exc.Message);
        }
    }

    private readonly Dictionary<string, List<int>> _userBanCache = new();

    private readonly Dictionary<string, Timer> _userJobBanPostTimers = new();

    public async Task PostUserJobBanInfo(int banId, string? targetUsername)
    {
        if (!string.IsNullOrWhiteSpace(targetUsername))
        {
            AddUserJobBanToCache(banId, targetUsername);
            AddUserJobBanTimer(targetUsername);
        }
    }

    private void AddUserJobBanTimer(string targetUsername)
    {
        if (!_userJobBanPostTimers.TryGetValue(targetUsername, out var timer))
        {
            timer = new();
            //timer.Elapsed += _ =>
            //{

            //};
        }
    }

    private void AddUserJobBanToCache(int banId, string targetUsername)
    {
        if (!_userBanCache.TryGetValue(targetUsername, out var cache))
        {
            cache = new List<int>();
            _userBanCache[targetUsername] = cache;
        }

        cache.Add(banId);
    }
}
