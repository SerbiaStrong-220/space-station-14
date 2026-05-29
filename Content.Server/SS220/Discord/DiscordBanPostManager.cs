// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;

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

        _cfg.OnValueChanged(CCVars220.DiscordLinkApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.DiscordLinkApiKey, v =>
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
            var url = $"{_apiUrl}/api/userBan/{banId}";

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
}
