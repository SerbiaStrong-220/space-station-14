// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
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
    ///     How many days of investigation bundles to keep. Bundles older than this are deleted when a round
    ///     starts. 0 keeps everything forever.
    /// </summary>
    /// <remarks>
    ///     Defaults to off, matching how the engine handles replays: it caps a single recording with
    ///     <c>replay.max_compressed_size</c> but never deletes one, leaving retention to whatever cleans up the
    ///     server's data directory. This exists because bundles are written every round and are much smaller, so
    ///     the sensible policy is a time window rather than a size cap — but which window is an operator's call,
    ///     and silently deleting evidence a server was relying on would be worse than filling a disk slowly.
    /// </remarks>
    public static readonly CVarDef<int> InvestigationRetentionDays =
        CVarDef.Create("investigation.retention_days", 0, CVar.SERVERONLY);

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
    ///     How often, in seconds, every tracked character is re-fingerprinted and re-snapshotted if it changed.
    /// </summary>
    /// <remarks>
    ///     This is the backstop sweep, covering changes no equipment event reports. Loadout changes that do raise
    ///     an event are picked up within <see cref="InvestigationDirtyInterval"/> instead, so this can stay slow.
    /// </remarks>
    public static readonly CVarDef<float> InvestigationCharacterInterval =
        CVarDef.Create("investigation.character_interval", 10f, CVar.SERVERONLY);

    /// <summary>
    ///     How often, in seconds, characters with a known equipment change are re-snapshotted.
    /// </summary>
    /// <remarks>
    ///     Bounds how stale a loadout record can be after someone picks something up. Kept above a single tick so
    ///     that a burst of equipment events — emptying a bag, a full loadout swap — collapses into one snapshot
    ///     rather than one per item.
    /// </remarks>
    public static readonly CVarDef<float> InvestigationDirtyInterval =
        CVarDef.Create("investigation.dirty_interval", 0.25f, CVar.SERVERONLY);

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
