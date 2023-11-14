// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Medicine.Injury.Components;

/// <summary>
/// 
/// </summary>

[RegisterComponent]
public sealed partial class InjuriesContainerComponent : Component
{
    [DataField("innerInjuries")]
    public List<EntityUid> InnerInjuries = new();
    [DataField("outerInjuries")]
    public List<EntityUid> OuterInjuries = new();
}