// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LearningOnDoAfterStartWithComponent : BaseLearningOnDoAfterWithComponent;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LearningOnDoAfterEndWithComponent : BaseLearningOnDoAfterWithComponent;

public abstract partial class BaseLearningOnDoAfterWithComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SkillTreePrototype>, LearningInformation> Progress = new();
}
