using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

[CVarDefs]
public sealed class CCVars220
{
    /// <summary>
    /// Whether is bloom lighting eanbled or not
    /// </summary>
    public static readonly CVarDef<bool> BloomLightingEnabled =
        CVarDef.Create("bloom_lighting.enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// How Round End Titles are shown for player
    /// </summary>
    public static readonly CVarDef<RoundEndTitlesMode> RoundEndTitlesOpenMode =
        CVarDef.Create("round_end_titles.open_mode", RoundEndTitlesMode.Fullscreen, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether to rotate doors when map is loaded
    /// </summary>
    public static readonly CVarDef<bool> MigrationAlignDoors =
        CVarDef.Create("map_migration.align_doors", false, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Boosty link, used by Slepavend UI
    /// </summary>
    public static readonly CVarDef<string> InfoLinksBoosty =
        CVarDef.Create("infolinks.boosty", "", CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> TraitorDeathMatchStartingBalance =
        CVarDef.Create("game.traitor_deathmatch_starting_balance", 20, CVar.SERVER);

    /// <summary>
    /// Delay of ahelp messages for non-admins.
    /// </summary>
    public static readonly CVarDef<float> AdminAhelpMessageDelay =
        CVarDef.Create("admin.ahelp_message_delay", 5f, CVar.SERVERONLY);

    /// <summary>
    ///     Delay in seconds before first load of the discord sponsors data.
    /// </summary>
    public static readonly CVarDef<float> DiscordSponsorsCacheLoadDelaySeconds =
        CVarDef.Create("discord_sponsors_cache.load_delay_seconds", 10f, CVar.SERVERONLY);

    /// <summary>
    ///     Interval in seconds between refreshes of the discord sponsors data.
    /// </summary>
    public static readonly CVarDef<float> DiscordSponsorsCacheRefreshIntervalSeconds =
        CVarDef.Create("discord_sponsors_cache.refresh_interval_seconds", 60f * 60f * 4f, CVar.SERVERONLY);
}
