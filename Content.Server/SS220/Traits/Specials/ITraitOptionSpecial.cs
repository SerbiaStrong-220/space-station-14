// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Traits.Specials;

/// <summary>
/// Contract for JobSpecials that need trait option chosen in the profile editor.
/// </summary>
public interface ITraitOptionSpecial
{
    /// <summary>
    /// Applies special using the selected trait option, or default when none.
    /// </summary>
    void AfterEquip(EntityUid uid, string? option);
}
