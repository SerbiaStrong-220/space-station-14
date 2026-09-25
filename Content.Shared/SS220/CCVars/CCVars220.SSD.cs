// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    public static readonly CVarDef<float> AfkTeleportToCryo =
        CVarDef.Create("afk.teleport_to_cryo", 600f, CVar.SERVERONLY);

    public static readonly CVarDef<float> LateAfkTeleportToCryo =
        CVarDef.Create("afk.late_teleport_to_cryo", 1200f, CVar.SERVERONLY);

    /// <summary>
    /// After passing this time we remove entity from ssd queue in ANY case
    /// </summary>
    public static readonly CVarDef<float> SSDTimeOut =
        CVarDef.Create("afk.ssd_timeout", 1400f, CVar.SERVERONLY);
}
