// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.Pod;

[RegisterComponent]
public sealed partial class CultYoggPodComponent : Component
{
    public ContainerSlot MobContainer = default!;

    [Serializable, NetSerializable]
    public enum CultPodVisuals : byte
    {
        Inserted,
    }
}
