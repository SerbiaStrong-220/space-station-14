using Content.Shared.SS220.TraitorDynamics;

namespace Content.Server.SS220.TraitorDynamics;

[RegisterComponent]
public sealed partial class TraitorDynamicsComponent : Component
{
    [DataField]
    public DynamicPrototype? Dynamic;
}
