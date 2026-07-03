// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CustomColorableLayer;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class CustomColorableLayerComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ColorableVisualLayer AttachedColoredSpriteLayer = ColorableVisualLayer.CustomColor;

    [DataField]
    [AutoNetworkedField]
    public Color ColoredLayerColor = Color.White;
}

public enum ColorableVisualLayer : byte
{
    CustomBase = 0,
    CustomColor = 1
}
