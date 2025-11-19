// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    ///     Default severity for species bans
    /// </summary>
    public static readonly CVarDef<string> SpeciesBanDefaultSeverity =
        CVarDef.Create("admin.species_ban_default_severity", "medium", CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);
}
