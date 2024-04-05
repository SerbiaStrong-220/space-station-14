// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Corvax.CCCVars;
using Robust.Shared.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Text.Json;

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
            var url = $"{_apiUrl}/userBan/{banId}";

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
            _sawmill.Error($"Error while posting user ban. {exc.Message}");
        }
    }

    private readonly Dictionary<string, List<int>> _userBanCache = new();

    private readonly Dictionary<string, Timer> _userJobBanPostTimers = new();

    public async Task PostUserJobBanInfo(int banId, string? targetUserName)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(targetUserName))
            {
                AddUserJobBanToCache(banId, targetUserName);
                AddUserJobBanTimer(targetUserName);
            }
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error while cached user role ban. {exc.Message}");
        }
    }

    private void AddUserJobBanTimer(string targetUserName)
    {
        if (!_userJobBanPostTimers.TryGetValue(targetUserName, out var timer))
        {
            timer = new()
            {
                AutoReset = false
            };

            timer.Elapsed += async (sender, e) => await JobBanProccessComplete(targetUserName);

            _userJobBanPostTimers[targetUserName] = timer;
        }

        timer.Stop();

        timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
        timer.Start();
    }

    private async Task JobBanProccessComplete(string userName)
    {
        if (string.IsNullOrEmpty(_apiUrl))
        {
            return;
        }

        _userBanCache.Remove(userName, out var bans);

        if (bans is null)
        {
            return;
        }

        try
        {
            var url = $"{_apiUrl}/userBan/roleBan";

            var response = await _httpClient.PostAsync(url,
                new StringContent(
                    JsonSerializer.Serialize(bans),
                    Encoding.UTF8,
                    "application/json"));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorText = await response.Content.ReadAsStringAsync();

                _sawmill.Error(
                    "Failed to post user role ban: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);
            }
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error while posting user role ban. {exc.Message}");
        }
    }

    private void AddUserJobBanToCache(int banId, string targetUsername)
    {
        if (!_userBanCache.TryGetValue(targetUsername, out var cache))
        {
            cache = [];
            _userBanCache[targetUsername] = cache;
        }

        cache.Add(banId);
    }
}
