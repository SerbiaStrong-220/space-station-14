// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SuperMatter.Emitter;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SuperMatterEmitterExtensionComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public int EnergyToMatterRatio;
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public int PowerConsumption;
}
