// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SuperMatter.Ui;

[Serializable, NetSerializable]
public enum SuperMatterEmitterExtensionUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SuperMatterEmitterExtensionApply(int power, int ratio) : BoundUserInterfaceMessage
{
    public int PowerConsumption = power;
    public int EnergyToMatterRatio = ratio;
}
