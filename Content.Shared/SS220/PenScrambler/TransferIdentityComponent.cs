using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.PenScrambler;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TransferIdentityComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public NetEntity? Target;

    [DataField]
    public HumanoidAppearanceComponent? AppearanceComponent;
}
