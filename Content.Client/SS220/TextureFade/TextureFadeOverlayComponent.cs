// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.TextureFade;
using Robust.Shared.Utility;

namespace Content.Client.SS220.TextureFade;

/// <summary>
/// Component for automatic texture fade processing, you can still use <see cref="TextureFadeOverlay"/> directly.
/// You can use all this data fiels directly in code to enable/disable, set progress, etc.
/// </summary>
[RegisterComponent]
public sealed partial class TextureFadeOverlayComponent : SharedTextureFadeOverlayComponent
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsEnabled = false;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float FadeProgress = 1;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier Sprite = SpriteSpecifier.Invalid;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Modulate = Color.White;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ZIndex = 0;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProgressSpeed = 0;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinProgress = 0;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxProgress = 1;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PulseMagnitude = 0;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PulseRate = 0;

    public TextureFadeOverlay? Overlay;
}
