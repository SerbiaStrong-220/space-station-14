// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    public static readonly CVarDef<bool> InvestigationEnabled =
        CVarDef.Create("investigation.enabled", true, CVar.SERVERONLY);

    /// <summary>Directory within the server data directory that bundles are written to.</summary>
    public static readonly CVarDef<string> InvestigationDirectory =
        CVarDef.Create("investigation.directory", "investigation", CVar.SERVERONLY);

    /// <summary>Bundles older than this are deleted when a round starts. 0 keeps everything forever.</summary>
    public static readonly CVarDef<int> InvestigationRetentionDays =
        CVarDef.Create("investigation.retention_days", 0, CVar.SERVERONLY);

    /// <summary>Seconds between position samples. Dominates bundle size by roughly 250:1.</summary>
    public static readonly CVarDef<float> InvestigationPositionInterval =
        CVarDef.Create("investigation.position_interval", 0.5f, CVar.SERVERONLY);

    /// <summary>Tiles moved before a new row is written. Grid and container changes ignore this.</summary>
    public static readonly CVarDef<float> InvestigationPositionEpsilon =
        CVarDef.Create("investigation.position_epsilon", 0.15f, CVar.SERVERONLY);

    /// <summary>Seconds between checks for dirty navmap chunks.</summary>
    public static readonly CVarDef<float> InvestigationNavMapInterval =
        CVarDef.Create("investigation.navmap_interval", 1f, CVar.SERVERONLY);

    /// <summary>Seconds between backstop sweeps that re-fingerprint every tracked character.</summary>
    public static readonly CVarDef<float> InvestigationCharacterInterval =
        CVarDef.Create("investigation.character_interval", 10f, CVar.SERVERONLY);

    /// <summary>Seconds between re-snapshots of characters with a known equipment change.</summary>
    public static readonly CVarDef<float> InvestigationDirtyInterval =
        CVarDef.Create("investigation.dirty_interval", 0.25f, CVar.SERVERONLY);

    /// <summary>Seconds between flushes of buffered rows to disk.</summary>
    public static readonly CVarDef<float> InvestigationFlushInterval =
        CVarDef.Create("investigation.flush_interval", 5f, CVar.SERVERONLY);

    /// <summary>Storage recursion depth. 1 is the backpack itself, 2 is the backpack and its contents.</summary>
    public static readonly CVarDef<int> InvestigationStorageDepth =
        CVarDef.Create("investigation.storage_depth", 2, CVar.SERVERONLY);
}
