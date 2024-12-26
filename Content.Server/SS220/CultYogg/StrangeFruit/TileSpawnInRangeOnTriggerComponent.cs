// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.CultYogg.StrangeFruit;

[RegisterComponent]
public sealed partial class TileSpawnInRangeOnTriggerComponent : Component
{
    [DataField("kudzuProtoId")]
    public string KudzuProtoId;

    [DataField("quantityInTile")]
    public int QuantityInTile = 1;

    [DataField("range")]
    public int Range = 1;
}
