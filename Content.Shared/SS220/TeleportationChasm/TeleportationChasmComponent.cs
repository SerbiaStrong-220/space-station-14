// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.TeleportationChasm;

/// <summary>
///     Marks a component that will cause entities to fall into them on a step trigger activation
/// </summary>
/// 
[NetworkedComponent, RegisterComponent, /*Access(typeof(ChasmSystem))*/]
public sealed partial class TeleportationChasmComponent : Component
{
    /// <summary>
    ///     Sound that should be played when an entity falls into the chasm
    /// </summary>
    [DataField]
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
}
