// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SmartGasMask.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SmartGasMask.Events;

[Serializable, NetSerializable]
public sealed class SmartGasMaskMessage : BoundUserInterfaceMessage
{
    public ProtoId<AlertSmartGasMaskPrototype> ProtoId;

    public SmartGasMaskMessage(ProtoId<AlertSmartGasMaskPrototype> protoId)
    {
        ProtoId = protoId;
    }
}

[Serializable, NetSerializable]
public enum SmartGasMaskUiKey : byte
{
    Key
}

