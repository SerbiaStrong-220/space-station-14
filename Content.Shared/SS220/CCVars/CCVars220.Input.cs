using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// Whether is bloom lighting eanbled or not
    /// </summary>
    public static readonly CVarDef<bool> TryToPickUpStorageInSuitContainer =
        CVarDef.Create("smart_equip.try_to_pickup_suitstorage_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ControlIgnoreGunInteraction =
        CVarDef.Create("control.ignore_gun_interaction", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ControlVanillaAim =
        CVarDef.Create("control.vanilla_aim", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
