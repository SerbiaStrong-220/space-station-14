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
}
