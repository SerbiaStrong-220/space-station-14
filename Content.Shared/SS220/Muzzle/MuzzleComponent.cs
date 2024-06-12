// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Muzzle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMuzzleSystem))]
/// <summary>
/// Added to clothing entities that should block the wearer's vocals emotions
/// </summary>
public sealed partial class MuzzleComponent : Component
{
}
