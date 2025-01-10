using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

[CVarDefs]
public sealed class CCVars220Performance
{
    /// <summary>
    /// Cvar which turns off most of player generatish sounds like steps, telephones and etc. Do not affect TTS.
    /// </summary>
    public static readonly CVarDef<bool> LessSoundSources =
        CVarDef.Create("audio.less_sound_sources", false, CVar.SERVER | CVar.REPLICATED, "serve to turn off most of player generatish sounds like steps, telephones and etc. Do not affect TTS.");
}
