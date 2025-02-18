using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Contractor;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ContractorPdaComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<NetEntity, ContractorContract> Contracts = new();

    [DataField]
    [AutoNetworkedField]
    public NetEntity? CurrentContractEntity;

    [DataField]
    [AutoNetworkedField]
    public ContractorContract? CurrentContractData;

    [DataField]
    [AutoNetworkedField]
    public Dictionary<string, ContractorItemData> AvailableItems = new();

    [DataField]
    [AutoNetworkedField]
    public NetEntity? PdaOwner;
}
