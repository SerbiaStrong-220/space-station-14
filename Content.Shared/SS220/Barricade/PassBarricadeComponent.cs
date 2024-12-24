
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Barricade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBarricadeSystem))]
public sealed partial class PassBarricadeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<EntityUid, bool> CollideBarricades = new();
}
