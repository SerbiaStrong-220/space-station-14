// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>Fraction of crew granted roundstart virus immunity.</summary>
    public static readonly CVarDef<float> PathologyImmunityFraction =
        CVarDef.Create("pathology.immunity_fraction", 0.05f, CVar.SERVER | CVar.ARCHIVE);
}
