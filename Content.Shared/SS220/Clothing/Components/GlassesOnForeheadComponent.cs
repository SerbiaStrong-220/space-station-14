// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Clothing.Components;

/// <summary>
/// Glasses that can be worn directly in the head or eyes slot.
/// Their optical protection is inactive in the head slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GlassesOnForeheadComponent : Component;
