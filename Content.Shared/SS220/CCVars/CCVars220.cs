using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

[CVarDefs]
public sealed partial class CCVars220
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
    /// (SS220) AHelp sound volume.
    /// </summary>
    public static readonly CVarDef<float> AHelpVolume =
        CVarDef.Create("ahelp.volume", 0.50f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Delay Between raising the networked event <see cref="SuperMatterStateUpdate"/>.
    /// </summary>
    public static readonly CVarDef<float> SuperMatterUpdateNetworkDelay =
        CVarDef.Create("network.superMatter_update_delay", 1f, CVar.SERVER | CVar.REPLICATED);

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

    public static readonly CVarDef<bool> DelayEnabled =
        CVarDef.Create("delay.enabled", false, CVar.NOTIFY | CVar.REPLICATED);

    public static readonly CVarDef<bool> AfkTimeKickEnabled =
        CVarDef.Create("afk.time_kick_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<float> AfkTimeKick =
        CVarDef.Create("afk.time_kick", 600f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AfkTeleportToCryo =
        CVarDef.Create("afk.teleport_to_cryo", 1800f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AfkActivityMessageInterval =
        CVarDef.Create("afk.activity_message_interval", 20f, CVar.CLIENTONLY | CVar.CHEAT);

    /// <summary>
    ///     Controls whether the server will deny any players that are not whitelisted in the Prime DB.
    /// </summary>
    public static readonly CVarDef<bool> PrimelistEnabled =
        CVarDef.Create("primelist.enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Automatically start a map vote after each round restart
    /// </summary>
    public static readonly CVarDef<bool> AutoMapVote =
        CVarDef.Create("vote.auto_map_vote", false, CVar.SERVERONLY);

    /// <summary>
    /// Is discord account link requiere.
    /// </summary>
    public static readonly CVarDef<bool> DiscordLinkRequired =
        CVarDef.Create("discord_auth.link_requierd", false, CVar.SERVERONLY);

    /// <summary>
    /// URL for check account link.
    /// </summary>
    public static readonly CVarDef<string> DiscordLinkApiUrl =
        CVarDef.Create("discord_auth.link_url", "", CVar.SERVERONLY);

    /// <summary>
    /// Key of account check service.
    /// </summary>
    public static readonly CVarDef<string> DiscordLinkApiKey =
        CVarDef.Create("discord_auth.link_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Allow enter server with bypass link check.
    /// </summary>
    public static readonly CVarDef<bool> ByPassDiscordLinkCheck =
        CVarDef.Create("discord_auth.bypass_check", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// How different is the game year from the real one
    /// </summary>
    public static readonly CVarDef<int> GameYearDelta =
        CVarDef.Create("date.game_year_delta", 544, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// How many sponsors can connect to the server beyond the player limit
    /// </summary>
    public static readonly CVarDef<int> MaxSponsorsBypass =
        CVarDef.Create("game.max_sponsors_bypass", 10, CVar.SERVER);
}
