// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.CultYogg.StrangeFruit;

[RegisterComponent]
public sealed partial class TileSpawnInRangeOnTriggerComponent : Component
{
    [DataField]
    public string KudzuProtoId;

    [DataField]
    public int QuantityInTile = 1;

    [DataField]
    public int Range = 1;
}
