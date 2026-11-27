using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

[Serializable, NetSerializable]
public sealed class ContractorExecutionBoundUserInterfaceState(
    bool? isEnabledExecution = null,
    bool? isEnabledPosition = null,
    float? blockExecutionTime = null,
    float? blockPositionsTime = null)
    : BoundUserInterfaceState
{
    public bool? IsEnabledExecution = isEnabledExecution;
    public bool? IsEnabledPosition = isEnabledPosition;
    public float? BlockExecutionTime = blockExecutionTime;
    public float? BlockPositionsTime = blockPositionsTime;
}

[Serializable, NetSerializable]
public sealed class ContractorUpdateStatsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ContractorExecutionButtonPressedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ContractorNewContractAcceptedMessage(
    NetEntity contractEntity,
    ContractorContract contractData,
    FixedPoint2 tcReward,
    NetEntity warpPointEntity)
    : BoundUserInterfaceMessage
{
    public NetEntity ContractEntity = contractEntity;
    public ContractorContract ContractData = contractData;
    public FixedPoint2 TcReward = tcReward;
    public NetEntity WarpPointEntity = warpPointEntity;
}

[Serializable, NetSerializable]
public sealed class ContractorAbortContractMessage(NetEntity contractEntity) : BoundUserInterfaceMessage
{
    public NetEntity ContractEntity = contractEntity;
}

[Serializable, NetSerializable]
public sealed class ContractorHubBuyItemMessage(string item, ContractorItemData data) : BoundUserInterfaceMessage
{
    public string Item = item;
    public ContractorItemData Data = data;
}

[Serializable, NetSerializable]
public sealed class ContractorWithdrawTcMessage(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public FixedPoint2 Amount = amount;
}

