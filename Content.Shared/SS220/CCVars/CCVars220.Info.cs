using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Description of the game server in the launcher, which can hold multiple of them!
    /// </summary>
    public static readonly CVarDef<string> GameDescList =
        CVarDef.Create("game.desc_list", "0.1|текст;0.2|текст2", CVar.SERVERONLY);

    /// <summary>
    /// Turning off usage of description list
    /// </summary>
    public static readonly CVarDef<bool> GameDescListEnabled =
        CVarDef.Create("game.desc_list_enabled", false, CVar.SERVERONLY);

    /// <summary>
    /// Interval when new description will be shuffled (in seconds)
    /// </summary>
    public static readonly CVarDef<float> GameDescListChangeInterval =
        CVarDef.Create("game.desc_list_change_interval", 20f, CVar.SERVERONLY);
}
