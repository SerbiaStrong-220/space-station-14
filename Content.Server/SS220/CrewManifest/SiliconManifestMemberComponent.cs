// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.CrewManifest;

/// <summary>
///     Marks a borg as manually added to a station's crew manifest.
/// </summary>
[RegisterComponent]
public sealed partial class SiliconManifestMemberComponent : Component
{
    /// <summary>
    ///     Station whose manifest this borg is listed on.
    /// </summary>
    public EntityUid Station = EntityUid.Invalid;
}

