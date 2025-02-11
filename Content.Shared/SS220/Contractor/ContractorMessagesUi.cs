using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

[Serializable, NetSerializable]
public sealed class ContractorExecutionBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool IsEnabled;

    public ContractorExecutionBoundUserInterfaceState(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

[Serializable, NetSerializable]
public sealed class ContractorUpdateStatsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ContractorCompletedContractMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ContractorExecutionButtonPressedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ContractorNewContractAcceptedMessage : BoundUserInterfaceMessage
{
    public NetEntity ContractEntity;
    public ContractorContract ContractData;
    public FixedPoint2 TcReward;
    public NetEntity WarpPointEntity;

    public ContractorNewContractAcceptedMessage(NetEntity contractEntity, ContractorContract contractData, FixedPoint2 tcReward, NetEntity warpPointEntity)
    {
        ContractEntity = contractEntity;
        ContractData = contractData;
        TcReward = tcReward;
        WarpPointEntity = warpPointEntity;
    }
}

[Serializable, NetSerializable]
public sealed class ContractorAbortContractMessage : BoundUserInterfaceMessage
{
    public NetEntity ContractEntity;

    public ContractorAbortContractMessage(NetEntity contractEntity)
    {
        ContractEntity = contractEntity;
    }
}

[Serializable, NetSerializable]
public sealed class ContractorHubBuyItemMessage : BoundUserInterfaceMessage
{
    public string Item;
    public FixedPoint2 Price;

    public ContractorHubBuyItemMessage(string item, FixedPoint2 price)
    {
        Item = item;
        Price = price;
    }
}

[Serializable, NetSerializable]
public sealed class ContractorWithdrawTcMessage : BoundUserInterfaceMessage
{
    public FixedPoint2 Amount;

    public ContractorWithdrawTcMessage(FixedPoint2 amount)
    {
        Amount = amount;
    }
}

