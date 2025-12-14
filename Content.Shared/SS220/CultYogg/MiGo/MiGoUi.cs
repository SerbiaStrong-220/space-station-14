// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Buildings;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.MiGo;

[Serializable, NetSerializable]
public sealed class MiGoErectBuiState : BoundUserInterfaceState
{
    public List<CultYoggBuildingPrototype> Buildings = [];
}

[Serializable, NetSerializable]
public sealed class MiGoTeleportBuiState : BoundUserInterfaceState
{
    public List<(string, NetEntity)> Warps = [];
}

[Serializable, NetSerializable]
public enum MiGoUiKey : byte
{
    Erect,
    Plant,
    Teleport
}
