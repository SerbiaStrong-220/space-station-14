using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public partial class CCVars220
{
    /// <summary>
    ///     Whether the played map memory is enabled
    /// </summary>
    public static readonly CVarDef<bool> GamePlayedMapMemory =
        CVarDef.Create("game.played_map_memory", true, CVar.SERVERONLY);

    /// <summary>
    ///     The depth of the queue of played maps.
    /// </summary>
    public static readonly CVarDef<int> GamePlayedMapMemoryDepth =
        CVarDef.Create("game.played_map_memory_depth", 1, CVar.SERVERONLY);
}
