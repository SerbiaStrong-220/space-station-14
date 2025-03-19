
using Content.Shared.SS220.Virology.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Virology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VirusCarrierComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<VirusPrototype>))]
    public List<string> Carries = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<VirusPrototype>))]
    public List<string> Immunity = new();
}
