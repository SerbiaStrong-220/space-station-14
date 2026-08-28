// SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Cooking;

/// <summary>
/// Added to an entity while a cooking source is actively cooking it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeingCookedComponent : Component;
