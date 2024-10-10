// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.UserInterface;

public sealed class SpriteButton : BaseButton
{
    public const string StylePropertySprite = "sprite";
    public const string StylePseudoClassNormal = "normal";
    public const string StylePseudoClassHover = "hover";
    public const string StylePseudoClassDisabled = "disabled";
    public const string StylePseudoClassPressed = "pressed";

    public StatesStyleSet StatesOverride
    {
        get => _statesOverride;
        set
        {
            _statesOverride = value;
            RefreshTextureRect();
        }
    }
    public Vector2 SpriteScale
    {
        get => _textureRect.DisplayRect.TextureScale;
        set => _textureRect.DisplayRect.TextureScale = value;
    }
    public TextureRect.StretchMode SpriteStretch
    {
        get => _textureRect.DisplayRect.Stretch;
        set => _textureRect.DisplayRect.Stretch = value;
    }
    public SpriteSpecifier? SpriteFromStyle { get; private set; }
    public AnimatedTextureRect TextureRect => _textureRect;

    private readonly AnimatedTextureRect _textureRect;
    private StatesStyleSet _statesOverride;

    public SpriteButton()
    {
        _textureRect = new();
        AddChild(_textureRect);
    }

    protected override void DrawModeChanged()
    {
        SetOnlyStylePseudoClass(DrawMode switch
        {
            DrawModeEnum.Normal => StylePseudoClassNormal,
            DrawModeEnum.Pressed => StylePseudoClassPressed,
            DrawModeEnum.Hover => StylePseudoClassHover,
            DrawModeEnum.Disabled => StylePseudoClassDisabled,
            _ => throw new ArgumentOutOfRangeException(),
        });
    }

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();
        TryGetStyleProperty(StylePropertySprite, out SpriteSpecifier? sprite);
        SpriteFromStyle = sprite;
        RefreshTextureRect();
    }

    private void RefreshTextureRect()
    {
        var sprite = StatesOverride.FromDrawMode(DrawMode).Sprite;
        sprite ??= SpriteFromStyle;
        if (sprite is { })
        {
            _textureRect.SetFromSpriteSpecifier(sprite);
            _textureRect.Visible = true;
        }
        else
        {
            _textureRect.Visible = false;
        }
    }

    public struct StateStyle
    {
        public SpriteSpecifier? Sprite { get; set; }
    }

    public struct StatesStyleSet
    {
        public StateStyle Normal { get; set; }
        public StateStyle Pressed { get; set; }
        public StateStyle Hover { get; set; }
        public StateStyle Disabled { get; set; }

        public readonly StateStyle FromDrawMode(DrawModeEnum drawMode)
        {
            return drawMode switch
            {
                DrawModeEnum.Normal => Normal,
                DrawModeEnum.Pressed => Pressed,
                DrawModeEnum.Hover => Hover,
                DrawModeEnum.Disabled => Disabled,
                _ => Normal,
            };
        }
    }
}
