// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.TeleportationChasm;

/// <summary>
///     Marks an entity as a chasm that causes targets to fall when its step trigger activates.
/// </summary>

[RegisterComponent]
public sealed partial class TeleportationChasmComponent : Component
{
    /// <summary>
    ///     Sound that should be played when an entity falls into the chasm
    /// </summary>
    [DataField]
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");

    /// <summary>
    ///     Entities matching this filter are deleted instead of teleported.
    /// </summary>
    [DataField]
    public EntityWhitelist? DeleteTargetWhitelist;
}
