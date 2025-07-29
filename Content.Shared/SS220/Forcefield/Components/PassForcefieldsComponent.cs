using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Forcefield.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PassForcefieldsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<NetEntity> PassedForcefields = [];
}
