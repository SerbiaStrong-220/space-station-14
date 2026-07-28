using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    /// The player's emote wheel arrangement: one line per slot, emote prototype ids separated by commas.
    /// Empty means "not configured yet", which fills the wheel with whatever the player can currently use.
    /// </summary>
    /// <remarks>
    /// Deliberately a client preference rather than part of the character profile. The wheel is a UI
    /// arrangement like keybinds, not something the character is, and storing it client-side means edits
    /// apply immediately instead of only affecting the next character that spawns.
    /// </remarks>
    public static readonly CVarDef<string> EmoteWheelLoadout =
        CVarDef.Create("ui.emote_wheel_loadout", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE);
}
