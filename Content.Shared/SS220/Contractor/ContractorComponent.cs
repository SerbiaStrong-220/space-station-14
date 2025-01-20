using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ContractorComponent : Component
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
    public NetEntity? PdaEntity;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan OpenPortalTime;

    [DataField]
    [AutoNetworkedField]
    public int Reputation;

    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 AmountTc = FixedPoint2.Zero;

    [DataField]
    [AutoNetworkedField]
    public int ContractsCompleted;

    public readonly int ReputationAward = 2;

    public int MaxAvailableContracts = 5;
}

[Serializable]
[NetSerializable]
public struct ContractorContract
{
    public ProtoId<JobPrototype> Job;
    public List<(NetEntity Uid, string Location, FixedPoint2 TcReward, Difficulty Difficulty)> AmountPositions;
}
