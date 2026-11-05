// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DefibReviveAlive;

/// <summary>
/// For mobs without a critical threshold: a defibrillator brings them straight to the alive state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DefibReviveAliveComponent : Component;
