using Robust.Shared.GameStates;
using Content.Shared.Whitelist;

namespace Content.Shared.Spider;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSpiderSystem))]
public sealed partial class SpiderWebObjectComponent : Component
{
	[DataField]
	public EntityWhitelist? BarotraumaImmuneWhitelist; // SS220 Spider queen
}
