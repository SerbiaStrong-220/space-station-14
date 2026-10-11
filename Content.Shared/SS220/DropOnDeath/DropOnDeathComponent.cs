// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DropOnDeath;

/// <summary>
/// Drops this item from its wearer's hands or inventory when the wearer dies.
/// Does not affect items inside storage or hidden containers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DropOnDeathComponent : Component;
