using Robust.Shared.GameStates;

namespace Content.Shared.Lube;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LubricatedComponent : Component
{
    [DataField("remaining"), AutoNetworkedField]
    public int Remaining = 0;
}
