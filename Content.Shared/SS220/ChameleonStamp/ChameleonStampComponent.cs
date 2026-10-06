// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ChameleonStamp;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedChameleonStampSystem))]
public sealed partial class ChameleonStampComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? Prototype;

    [DataField]
    public List<EntProtoId> Variants = [];
}

[Serializable, NetSerializable]
public sealed class ChameleonStampBoundUserInterfaceState(EntProtoId? selectedId, List<EntProtoId> variants) : BoundUserInterfaceState
{
    public readonly EntProtoId? SelectedId = selectedId;
    public readonly List<EntProtoId> Variants = variants;
}

[Serializable, NetSerializable]
public sealed class ChameleonStampSelectedMessage(EntProtoId selectedId) : BoundUserInterfaceMessage
{
    public readonly EntProtoId SelectedId = selectedId;
}

[Serializable, NetSerializable]
public enum ChameleonStampUiKey : byte
{
    Key
}