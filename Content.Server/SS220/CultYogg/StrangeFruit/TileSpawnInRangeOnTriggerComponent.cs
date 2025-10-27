// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Trigger.Components.Effects;

namespace Content.Server.SS220.CultYogg.StrangeFruit;

[RegisterComponent]
public sealed partial class TileSpawnInRangeOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true)]
    public string ProtoId;

    [DataField]
    public int QuantityInTile = 1;

    [DataField]
    public int Range = 1;
}
