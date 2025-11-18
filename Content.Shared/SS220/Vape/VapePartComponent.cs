using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Vape;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VapePartComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public BaseVapePartData PartData;

    [DataField("rsi")]
    public SpriteSpecifier RSIForVape; // TODO SS220 Rename field

    [DataField]
    public Vector2? Offset;
}

[Serializable, NetSerializable, DataDefinition]
public abstract partial class BaseVapePartData
{
    [DataField]
    public float MaxDurability = 100f;

    [DataField]
    public float DurabilityConsumption = 0.03f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentDurability;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AtomizerPartData : BaseVapePartData
{
    [DataField]
    public float? EmaggedVolume;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CartridgePartData : BaseVapePartData
{
    [DataField]
    public float ConsumptionRate;
}

[Serializable, NetSerializable]
public enum VapeParts
{
    Atomizer = 1,
    Cartridge,
}
