// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HiddenInstalledImplantComponent : Component
{
    /// <summary>
    /// How goodly implant installed
    /// </summary>
    public HiddenInstallLevel InstallLevel;

    /// <summary>
    /// If implant can be found but still not visible
    /// </summary>
    public bool Hidden;
}

/// <summary>
/// Should much minimal required level to see implant
/// Note having required level does not guarantee that the implant will be detected
/// </summary>
public enum HiddenInstallLevel : int
{
    Easy = 2,
    Medium = 3,
    Hard = 4,
}

[ByRefEvent]
public record struct GetSubdermalInstallLevel(HiddenInstallLevel InstallLevel = HiddenInstallLevel.Easy, bool Hidden = false);
