using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether the lightweight investigation recorder is enabled.
    /// </summary>
    public static readonly CVarDef<bool> InvestigationEnabled =
        CVarDef.Create("investigation.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Directory within the server data directory that investigation bundles are written to.
    /// </summary>
    public static readonly CVarDef<string> InvestigationDirectory =
        CVarDef.Create("investigation.directory", "investigation", CVar.SERVERONLY);

    /// <summary>
    ///     How often, in seconds, tracked entity positions are sampled.
    /// </summary>
    /// <remarks>
    ///     Positions outweigh every other stream by roughly 250:1, so this is the one knob that decides
    ///     bundle size. 0.5s keeps a 2 hour round in the low tens of megabytes; 0.2s roughly triples it for
    ///     resolution no investigation has been shown to need.
    /// </remarks>
    public static readonly CVarDef<float> InvestigationPositionInterval =
        CVarDef.Create("investigation.position_interval", 0.5f, CVar.SERVERONLY);

    /// <summary>
    ///     Minimum distance, in tiles, a tracked entity must move before a new position row is written.
    ///     Grid changes and container changes are always written regardless of this threshold.
    /// </summary>
    public static readonly CVarDef<float> InvestigationPositionEpsilon =
        CVarDef.Create("investigation.position_epsilon", 0.15f, CVar.SERVERONLY);

    /// <summary>
    ///     How often, in seconds, grids are checked for dirty navmap chunks.
    /// </summary>
    public static readonly CVarDef<float> InvestigationNavMapInterval =
        CVarDef.Create("investigation.navmap_interval", 1f, CVar.SERVERONLY);

    /// <summary>
    ///     How often, in seconds, character loadouts are fingerprinted and re-snapshotted if they changed.
    /// </summary>
    public static readonly CVarDef<float> InvestigationCharacterInterval =
        CVarDef.Create("investigation.character_interval", 10f, CVar.SERVERONLY);

    /// <summary>
    ///     How often, in seconds, buffered rows are flushed to disk.
    /// </summary>
    public static readonly CVarDef<float> InvestigationFlushInterval =
        CVarDef.Create("investigation.flush_interval", 5f, CVar.SERVERONLY);

    /// <summary>
    ///     How deep to recurse into storage containers when snapshotting what a character is carrying.
    ///     1 means "the backpack itself", 2 means "the backpack and what is inside it".
    /// </summary>
    public static readonly CVarDef<int> InvestigationStorageDepth =
        CVarDef.Create("investigation.storage_depth", 2, CVar.SERVERONLY);
}
