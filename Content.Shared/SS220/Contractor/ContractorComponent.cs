using Content.Shared.FixedPoint;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is used for marking contractor
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ContractorComponent : Component
{
    /// <summary>
    /// Dictionary, where key is target and value is "details" of contract
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public Dictionary<NetEntity, ContractorContract> Contracts = new();

    [DataField]
    [AutoNetworkedField]
    public Dictionary<NetEntity, HumanoidCharacterProfile> Profiles = new();

    /// <summary>
    /// Used for marking, that contractor is currently with contract
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public NetEntity? CurrentContractEntity;

    /// <summary>
    /// Used for marking, that contractor is currently with contract
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ContractorContract? CurrentContractData;

    /// <summary>
    /// Current amount reputation
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int Reputation;

    /// <summary>
    /// Current amount telecrystals
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 AmountTc = FixedPoint2.Zero;

    /// <summary>
    /// Current amount successful completed contracts
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int ContractsCompleted;

    /// <summary>
    /// Used for marking, that pda already have "only one owner"
    /// </summary>
    [DataField]
    public EntityUid? PdaEntity;

    [DataField]
    [AutoNetworkedField]
    public float? BlockUntil;

    public readonly int ReputationAward = 2;

    public int MaxAvailableContracts = 5;
}

[Serializable]
[NetSerializable]
public struct ContractorContract
{
    public string Name;
    public ProtoId<JobPrototype> Job;
    public List<(NetEntity Uid, string Location, FixedPoint2 TcReward, Difficulty Difficulty)> AmountPositions;
}
