using Content.Shared.SS220.TraitorDynamics;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TraitorDynamics;

[RegisterComponent]
public sealed partial class TraitorDynamicsComponent : Component
{
    [DataField]
    public ProtoId<DynamicPrototype>? Dynamic;
}
