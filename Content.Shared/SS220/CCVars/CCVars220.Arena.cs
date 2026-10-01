// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    public static readonly CVarDef<int> ArenaActiveLimit =
        CVarDef.Create("arena.active_limit", 15, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> ArenaCreateCooldown =
        CVarDef.Create("arena.create_cooldown", 60, CVar.SERVERONLY | CVar.ARCHIVE);
}
