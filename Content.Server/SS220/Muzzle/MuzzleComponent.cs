// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Server.SS220.Muzzle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MuzzleSystem))]
/// <summary>
/// Added to entity that must block the vocal emotions of other entity
/// </summary>
public sealed partial class MuzzleComponent : Component
{
}
