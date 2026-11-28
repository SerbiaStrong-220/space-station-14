// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Body;

/// <summary>
/// On a body that crawls because it lost all supporting limbs: its <see cref="Content.Shared.Movement.Components.WormComponent"/>
/// was given by <see cref="LimbSystem"/>, so a new limb takes it back. A worm from elsewhere (e.g. an admin smite) is kept.
/// </summary>
[RegisterComponent]
[Access(typeof(LimbSystem))]
public sealed partial class LimbWormComponent : Component;
