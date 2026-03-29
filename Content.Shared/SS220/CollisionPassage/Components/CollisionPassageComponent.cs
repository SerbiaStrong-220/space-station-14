// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.CollisionPassage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CollisionPassageComponent : Component
{
	[DataField]
	public EntityWhitelist? Whitelist;

	[DataField]
	public bool AllowIncapacitatedMobs;
}
