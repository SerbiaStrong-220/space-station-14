using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Cvar indicates that the server is running to generate data for the wiki.
    /// Shutdowns the server after the generation is completed
    /// </summary>
    public static readonly CVarDef<bool> GenerateWikiDataRun =
        CVarDef.Create("autogen.generate_wiki_data_run", false, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<string> ChemicalsJsonSavePath =
        CVarDef.Create("autogen.chemicals_json_save_path", "chemicals_prototypes", CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<string> ReactionsJsonSavePath =
        CVarDef.Create("autogen.reactions_json_save_path", "reactions_prototypes", CVar.SERVER | CVar.SERVERONLY);

    /// <summary>
    /// Cvar indicates that the server is running to generate data for the webmap.
    /// Shutdowns the server after the generation is completed
    /// </summary>
    public static readonly CVarDef<bool> GenerateWebmapDataRun =
        CVarDef.Create("autogen.generate_webmap_data_run", false, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<string> StationsJsonSavePath =
        CVarDef.Create("autogen.stations_json_save_path", "stations", CVar.SERVER | CVar.SERVERONLY);
}
