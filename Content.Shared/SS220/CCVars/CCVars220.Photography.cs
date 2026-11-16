// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Period in seconds over which photo data and reserved map requests are counted.
    /// </summary>
    public static readonly CVarDef<float> PhotoRequestRateLimitPeriod =
        CVarDef.Create("photo.request_rate_limit_period", 1f, CVar.SERVERONLY);

    /// <summary>
    /// Maximum number of photo requests per player in one period.
    /// </summary>
    public static readonly CVarDef<int> PhotoRequestRateLimitCount =
        CVarDef.Create("photo.request_rate_limit_count", 2, CVar.SERVERONLY);
}
