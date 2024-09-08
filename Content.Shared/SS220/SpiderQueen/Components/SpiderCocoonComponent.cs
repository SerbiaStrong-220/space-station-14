// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent]
public sealed partial class SpiderCocoonComponent : Component
{
    [DataField("container", required: true)]
    public string CocoonContainerId = "cocoon";
}
