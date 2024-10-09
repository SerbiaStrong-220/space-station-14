namespace Content.Server.SS220.CultYogg.StrangeFruit.Components;

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
