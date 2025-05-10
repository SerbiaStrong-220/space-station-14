// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.SS220.StyleTools;
using Content.Client.SS220.UserInterface;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.CluwneComms.UI;

public sealed class CluwneCommsConsoleStyle : QuickStyle
{
    private readonly Color _color0 = new(188, 237, 100);
    private readonly Color _color1 = new(101, 150, 45);

    private readonly ResPath _textures = new("/Textures/SS220/Interface/CluwneComms/");
    private readonly ResPath _fonts = new("/Fonts/SS220/");

    protected override void CreateRules()
    {
        var balsamiqSans14 = VectorFont(_fonts / "BalsamiqSans/BalsamiqSans-Regular.ttf", 14);
        var balsamiqSans10 = VectorFont(_fonts / "BalsamiqSans/BalsamiqSans-Regular.ttf", 10);

        Builder
            .Element<Label>()
            .Prop("font", balsamiqSans14)
            .Prop("font-color", _color1);
        Builder
            .Element<RichTextLabel>()
            .Prop("font", balsamiqSans14)
            .Prop("font-color", _color1);
        Builder
            .Element<TextEdit>()
            .Prop("font", balsamiqSans14)
            .Prop("font-color", _color1)
            .Prop("cursor-color", _color1);
        Builder
            .Element<Label>()
            .Class("WindowFooterText")
            .Prop("font", balsamiqSans10)
            .Prop("font-color", _color1);

        Builder
            .Element<PanelContainer>()
            .Class("CluwneCommsPanel")
            .Prop("panel", StrechedStyleBoxTexture(Tex(_textures / "cluwne_comms_body.png")));

        Builder
            .Element<SpriteButton>()
            .Class("CluwneBigButton")
            .Prop("sprite", Sprite(_textures / "cluwne_comms_big_button.rsi", "normal"));
        Builder
            .Element<SpriteButton>()
            .Class("CluwneBigButton")
            .Pseudo("hover")
            .Prop("sprite", Sprite(_textures / "cluwne_comms_big_button.rsi", "hover"));
        Builder
            .Element<SpriteButton>()
            .Class("CluwneBigButton")
            .Pseudo("pressed")
            .Prop("sprite", Sprite(_textures / "cluwne_comms_big_button.rsi", "pressed"));

        var buttonBoxNormal = new StyleBoxTexture()
        {
            Texture = Tex(_textures / "cluwne_button.png"),
            PatchMarginLeft = 3,
            PatchMarginTop = 3,
            PatchMarginRight = 3,
            PatchMarginBottom = 3,
            TextureScale = Vector2.One * 2,
        };
        var buttonBoxInversed = new StyleBoxTexture(buttonBoxNormal)
        {
            Texture = Tex(_textures / "cluwne_button_pressed.png"),
        };
        var squareBoxBorder = new StyleBoxTexture(buttonBoxNormal)
        {
            Texture = Tex(_textures / "cluwne_square_border.png"),
        };
        var squareBoxInversed = new StyleBoxTexture(buttonBoxNormal)
        {
            Texture = Tex(_textures / "cluwne_square_pressed.png"),
        };

        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("normal")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxNormal);
        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("hover")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxInversed);
        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("pressed")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxInversed);
        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("disabled")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxInversed);

        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("hover")
            .Child<Label>()
            .Prop("font-color", _color0);
        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("pressed")
            .Child<Label>()
            .Prop("font-color", _color0);
        Builder
            .Element<Button>()
            .Class("button")
            .Pseudo("disabled")
            .Child<Label>()
            .Prop("font-color", _color0);

        Builder
            .Element<TabContainer>()
            .Prop("font", balsamiqSans14)
            .Prop("panel-stylebox", squareBoxBorder)
            .Prop("tab-stylebox", squareBoxInversed)
            .Prop("tab-font-color", _color0)
            .Prop("tab-stylebox-inactive", squareBoxBorder)
            .Prop("tab-font-color-inactive", _color1);

        Builder
            .Element<OptionButton>()
            .Class("button")
            .Pseudo("normal")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxNormal);
        Builder
            .Element<OptionButton>()
            .Class("button")
            .Pseudo("hover")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxNormal);
        Builder
            .Element<OptionButton>()
            .Class("button")
            .Pseudo("pressed")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxNormal);
        Builder
            .Element<OptionButton>()
            .Class("button")
            .Pseudo("disabled")
            .Prop("modulate-self", Color.White)
            .Prop("stylebox", buttonBoxNormal);

        Builder.Element<TextureRect>()
            .Class("optionTriangle")
            .Prop("modulate-self", _color1);

        Builder.Element<PanelContainer>()
            .Class("optionButtonBackground")
            .Prop("panel", squareBoxInversed);
    }
}
