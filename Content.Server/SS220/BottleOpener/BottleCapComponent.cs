// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.SS220.BottleOpener;

[RegisterComponent, Access(typeof(OpenableSystem))]
public sealed partial class BottleCapComponent : Component
{
    [DataField("bottleProtoypeID")]
    public string BottlePrototypeID;
}
