// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid;

namespace Content.Server.SS220.SlimePersonColorApplier;

[RegisterComponent]
public sealed partial class SlimePersonColorApplierComponent : Component
{
    [DataField]
    public Color BeforeSkinColor = new(0f, 0f, 0f, 0f);

    [DataField]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> BeforeCustomBaseLayers = new ();

}
