// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.BloomLight;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class BloomLightMaskComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<BloomMaskSpecifier> LightMasks = new()
    {
        new()
        {
            UseShader = true,
            Modulate = Color.White,
            Sprite = new SpriteSpecifier.Texture(new("SS220/BloomLight/Masks/lightmask_lamp_soft.png"))
        },
    };

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool UseLightColor;

    [DataField, AutoNetworkedField]
    public bool UseShader = true;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct BloomMaskSpecifier
{
    [DataField]
    public bool UseShader = true;

    [DataField]
    public bool Unshaded = false;

    [DataField]
    public Color Modulate = Color.White;

    [DataField]
    public SpriteSpecifier Sprite;
}
