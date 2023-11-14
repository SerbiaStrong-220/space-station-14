// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Medicine.Injure.Components;

/// <summary>
/// 
/// </summary>

[RegisterComponent]
public sealed partial class InjuredComponent : Component
{
    [DataField("innerInjuers")]
    public List<EntityUid> InnerInjures = new();
    [DataField("outterInjures")]
    public List<EntityUid> OutterInjures = new();
}