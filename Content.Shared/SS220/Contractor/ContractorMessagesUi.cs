using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

[Serializable, NetSerializable]
public sealed class ContractorGenerateContractsMessage : BoundUserInterfaceMessage
{
    public ContractorGenerateContractsMessage() { }
}

[Serializable, NetSerializable]
public sealed class ContractorUpdateStatsMessage : BoundUserInterfaceMessage
{
    public ContractorUpdateStatsMessage() { }
}

[Serializable, NetSerializable]
public sealed class ContractorCompletedContractMessage : BoundUserInterfaceMessage
{
    public ContractorCompletedContractMessage() { }
}

[Serializable, NetSerializable]
public sealed class ContractorUpdateButtonStateMessage : BoundUserInterfaceMessage
{
    public bool IsEnabled { get; }

    public ContractorUpdateButtonStateMessage(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

[Serializable, NetSerializable]
public sealed class ContractorNewContractAcceptedMessage : BoundUserInterfaceMessage
{
    public NetEntity ContractEntity;
    public ContractorContract ContractData;

    public ContractorNewContractAcceptedMessage(NetEntity contractEntity, ContractorContract contractData)
    {
        ContractEntity = contractEntity;
        ContractData = contractData;
    }
}

[Serializable, NetSerializable]
public sealed class ContractorExecutionButtonPressedMessage : BoundUserInterfaceMessage
{

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

