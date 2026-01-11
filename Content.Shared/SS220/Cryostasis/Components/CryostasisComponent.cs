using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Cryostasis.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CryostasisComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FastInjectionMultiply = 5f;
}
