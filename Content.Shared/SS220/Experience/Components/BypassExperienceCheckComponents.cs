// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// This is used to mark entity which bypasses knowledge's checks
/// </summary>
[RegisterComponent, NetworkedComponent]
public abstract partial class BypassKnowledgeCheckComponent : Component { }

/// <summary>
/// This is used to mark entity which bypasses skill's checks
/// </summary>
[RegisterComponent, NetworkedComponent]
public abstract partial class BypassSkillCheckComponent : Component { }

