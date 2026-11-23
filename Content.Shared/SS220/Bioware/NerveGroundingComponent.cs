// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Bioware;

/// <summary>
/// Bioware of the nerves group: grounded nerves protect from electric shocks like insulated gloves.
/// Shocks that ignore insulation (the defibrillator) still pass.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NerveGroundingComponent : Component;
