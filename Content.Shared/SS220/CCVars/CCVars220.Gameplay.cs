// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    ///     All doafter delay multiplier
    /// </summary>
    public static readonly CVarDef<float> DoafterDelayModifier =
        CVarDef.Create("gameplay.doafter_delay_modifier", 0.75f, CVar.ARCHIVE | CVar.SERVER | CVar.REPLICATED);
}
