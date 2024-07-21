// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DarkReaper;

[RegisterComponent, NetworkedComponent]
/// <summary>
///     Dark Reaper will not be able to consume entity with this component
/// </summary>
public sealed partial class CannotBeConsumedComponent : Component
{
}
