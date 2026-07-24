// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>Min crew granted roundstart virus immunity.</summary>
    public static readonly CVarDef<int> VirologyImmuneCountMin =
        CVarDef.Create("virology.immune_count_min", 1, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>Max crew granted roundstart virus immunity.</summary>
    public static readonly CVarDef<int> VirologyImmuneCountMax =
        CVarDef.Create("virology.immune_count_max", 2, CVar.SERVER | CVar.ARCHIVE);
}
