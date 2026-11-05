using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Cvar which turns off most of player generatish sounds like steps, telephones and etc. Do not affect TTS.
    /// </summary>
    public static readonly CVarDef<bool> LessSoundSources =
        CVarDef.Create("audio.less_sound_sources", false, CVar.SERVER | CVar.REPLICATED, "serve to turn off most of player generatish sounds like steps, telephones and etc. Do not affect TTS.");

    /// <summary>
    /// Cvar which switches mob movement update from sequential to parallel.
    /// </summary>
    public static readonly CVarDef<bool> ParallelMoverUpdate =
        CVarDef.Create("performance.parallel_mover_update", false, CVar.SERVERONLY, "switches mob movement update from sequential to parallel");

    /// <summary>
    /// Cvar which actually changes number of batches in parallel mover update which can (and we use it) hardlimit amount of processing threads
    /// </summary>
    public static readonly CVarDef<int> ParallelMoverThreads =
        CVarDef.Create("performance.parallel_mover_threads", 4, CVar.SERVERONLY, "changes number of batches in parallel mover update");
}
