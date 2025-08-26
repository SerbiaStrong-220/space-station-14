// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared;
using Robust.Shared.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class AuthBannedGuidManager
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private ISawmill _sawmill = Logger.GetSawmill("authBannedGuidManager");

    private Dictionary<Guid, string?> _guidToName = new();
    private readonly HttpClient _httpClient = new();

    private const int MaxCached = 50;
    private const string UserNameField = "userName";

    public void ResetCache() => _guidToName.Clear();

    public async Task<string?> TryGetPlayerName(Guid? guid)
    {
        if (guid is null)
            return null;

        if (_guidToName.TryGetValue(guid.Value, out var cachedName))
            return cachedName;

        if (_guidToName.Count == MaxCached)
            return null;

        var result = await TryGetPlayerNameInternal(guid.Value);
        _guidToName.Add(guid.Value, result);

        return result;
    }

    private async Task<string?> TryGetPlayerNameInternal(Guid guid)
    {
        var authServer = _config.GetCVar(CVars.AuthServer);
        var url = $"{authServer}api/query/userid?userid={guid}";

        var response = await _httpClient.GetAsync(url, CancellationToken.None);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await JsonDocument.ParseAsync(response.Content.ReadAsStream());
        var root = content.RootElement;

        if (!root.TryGetProperty(UserNameField, out var data))
            return null;

        return data.ToString();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
