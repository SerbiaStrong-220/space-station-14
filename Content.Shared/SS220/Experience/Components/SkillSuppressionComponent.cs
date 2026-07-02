// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[RegisterComponent, NetworkedComponent]
public sealed partial class SkillSuppressionComponent : Component
{
    /// <summary>
    /// Per skill tree, the override that was in place before suppression replaced it (null = none).
    /// On removal each tree is restored to its saved value instead of blanket-clearing, so an
    /// override another system installed isn't clobbered.
    /// </summary>
    [ViewVariables]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeInfo?> SavedOverrides = new();
}
