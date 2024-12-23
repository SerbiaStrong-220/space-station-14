using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
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
    public int Reputation = 0;

    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 AmountTc = FixedPoint2.Zero;

    [DataField]
    [AutoNetworkedField]
    public int ContractsCompleted = 0;

    public readonly int ReputationAward = 2;
}

[Serializable]
[NetSerializable]
public struct ContractorContract
{
    public string Job;
    public List<(NetEntity Uid, string Location, FixedPoint2 TcReward, string Difficulty)> AmountPositions;
}
