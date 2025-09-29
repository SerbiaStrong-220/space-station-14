using Robust.Shared.Utility;

namespace Content.Shared.SS220.PinnableUI;

[RegisterComponent]
public sealed partial class PinnableUIComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/VerbIcons/verb_pin.png"));
}
