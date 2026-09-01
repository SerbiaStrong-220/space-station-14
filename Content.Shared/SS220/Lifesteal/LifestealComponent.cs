using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Lifesteal;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class LifestealComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Lifesteal = 5f;
}
