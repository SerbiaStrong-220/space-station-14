using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinid.Components;

/// <summary>
/// Opts an entity into weapon ground recoil. Networked so the client can predict the push.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunRecoilModifierComponent : Component;
