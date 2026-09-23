// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Plays sounds at the target's departure and arrival locations during teleportation.
/// </summary>
[RegisterComponent]
public sealed partial class TeleportSoundComponent : Component
{
    /// <summary>
    ///     Sound played at the target's departure location.
    /// </summary>
    [DataField]
    public SoundSpecifier? DepartureSound;

    /// <summary>
    ///     Sound played at the target's arrival location.
    /// </summary>
    [DataField]
    public SoundSpecifier? ArrivalSound;
}
