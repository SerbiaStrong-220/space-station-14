// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Removes the teleportation device entity after its use is exhausted.
/// </summary>
[RegisterComponent]
public sealed partial class DeleteOnTeleportedComponent : Component
{
    /// <summary>
    ///     Number of uses before deletion
    /// </summary>
    [DataField]
    public int Amount = 1;

    /// <summary>
    ///     Sound played when deleting
    /// </summary>
    [DataField]
    public SoundSpecifier? DeleteSound;
}
