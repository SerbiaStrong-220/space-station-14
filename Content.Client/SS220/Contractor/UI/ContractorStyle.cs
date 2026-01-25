using System.Numerics;
using Content.Client.SS220.StyleTools;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Contractor.UI;

public sealed class ContractorStyle : QuickStyle
{
    private readonly IResourceCache _cache = IoCManager.Resolve<IResourceCache>();

    private StyleBoxTexture CreateStyleBox(string texturePath, float scale = 1.5f, float marginLeft = 0f, float marginRight = 0f, float marginTop = 0f, float marginBottom = 0f)
    {
        return new StyleBoxTexture()
        {
            Texture = Tex(texturePath),
            TextureScale = new Vector2(scale),
            PatchMarginLeft = marginLeft,
            PatchMarginRight = marginRight,
            PatchMarginTop = marginTop,
            PatchMarginBottom = marginBottom,
        };
    }

    protected override void CreateRules()
    {
        Builder
            .Element<PanelContainer>()
            .Class("ContractorPanel")
            .Prop(PanelContainer.StylePropertyPanel,
                new StyleBoxTexture()
                {
                    Texture = Tex("/Textures/SS220/Interface/Contractor/contractor-pda-body.png"),
                    TextureScale = new(2f),
                });

        Builder
            .Element<PanelContainer>()
            .Class("ContractorBackground")
            .Prop(PanelContainer.StylePropertyPanel,
                new StyleBoxTexture()
                {
                    Mode = StyleBoxTexture.StretchMode.Tile,
                    Texture = Tex("/Textures/SS220/Interface/Contractor/contractor-pda-background.png"),
                    TextureScale = new(2f),
                });

        Builder
            .Element<PanelContainer>()
            .Class("ContractorContractsLinePanel")
            .Prop(PanelContainer.StylePropertyPanel,
                new StyleBoxTexture()
                {
                    Texture = Tex(
                        "/Textures/SS220/Interface/Contractor/contractor-pda-panel-contrast.png"),
                    PatchMarginLeft = 1f,
                    PatchMarginRight = 1f,
                    PatchMarginTop = 1f,
                    PatchMarginBottom = 1f,
                    TextureScale = new Vector2(2f),
                    Mode = StyleBoxTexture.StretchMode.Stretch
                });

        Builder
            .Element<Button>()
            .Class("ContractorButtonStyle")
            .Prop(ContainerButton.StylePropertyStyleBox,
                CreateStyleBox("/Textures/SS220/Interface/Contractor/contractor-pda-tab-disabled.png",
                    marginLeft: 2f,
                    marginRight: 2f));

        Builder
            .Element<Button>()
            .Class("ContractorButtonStyle")
            .Pseudo(ContainerButton.StylePseudoClassNormal)
            .Prop(Control.StylePropertyModulateSelf, Color.White);

        Builder
            .Element<Button>()
            .Class("ContractorButtonStyle")
            .Pseudo(ContainerButton.StylePseudoClassPressed)
            .Prop(ContainerButton.StylePropertyStyleBox,
                CreateStyleBox("/Textures/SS220/Interface/Contractor/contractor-pda-tab-normal.png", marginLeft: 2f, marginRight: 2f))
            .Prop(Control.StylePropertyModulateSelf, Color.White);

        Builder
            .Element<Label>()
            .Class("ContractorLabelStyle")
            .Prop(Label.StylePropertyFontColor, Color.FromHex("#93abb2"))
            .Prop(Label.StylePropertyFont, new VectorFont(_cache.GetResource<FontResource>("/Fonts/SS220/Tuffy/Tuffy-Regular.ttf"), 10))
            .Build();

        Builder
            .Element<RichTextLabel>()
            .Class("ContractorRichLabelStyle")
            .Prop(Label.StylePropertyFontColor, Color.FromHex("#93abb2"))
            .Prop(Label.StylePropertyFont, new VectorFont(_cache.GetResource<FontResource>("/Fonts/SS220/Tuffy/Tuffy-Regular.ttf"), 9))
            .Build();

        Builder
            .Element<PanelContainer>()
            .Class("ContractorContractsImage")
            .Prop(PanelContainer.StylePropertyPanel, StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Contractor/contractor-pda-contracts-image.png")));

        Builder
            .Element<PanelContainer>()
            .Class("ContractorHubImage")
            .Prop(PanelContainer.StylePropertyPanel, StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Contractor/contractor-pda-hub-image.png")));

        Builder
            .Element<PanelContainer>()
            .Class("ContractorDividerPanel")
            .Prop(PanelContainer.StylePropertyPanel,
                new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#647b88"), ContentMarginBottomOverride = 3,
                    ContentMarginLeftOverride = 3
                });

        Builder
            .Element<PanelContainer>()
            .Class("ContractsPanelFilled")
            .Prop(PanelContainer.StylePropertyPanel, CreateStyleBox("/Textures/SS220/Interface/Contractor/contractor-pda-panel-filled.png", marginLeft: 1f, marginRight: 1f, marginBottom: 1f, marginTop: 1f))
            .Prop(PanelContainer.StylePropertyModulateSelf, Color.FromHex("#223140"));

        Builder
            .Element<Button>()
            .Class("ContractorExecutionButton")
            .Prop(ContainerButton.StylePropertyStyleBox,
                CreateStyleBox("/Textures/SS220/Interface/Contractor/contractor-pda-button-normal.png",
                    marginLeft: 2f,
                    marginRight: 2f,
                    marginTop: 2f,
                    marginBottom: 2f));

        Builder
            .Element<Button>()
            .Class("ContractorExecutionButton")
            .Pseudo(ContainerButton.StylePseudoClassDisabled)
            .Prop(ContainerButton.StylePropertyStyleBox,
                CreateStyleBox("/Textures/SS220/Interface/Contractor/contractor-pda-button-disabled.png",
                    marginLeft: 2f,
                    marginRight: 2f,
                    marginTop: 2f,
                    marginBottom: 2f));

        Builder
            .Element<Button>()
            .Class("ContractorExecutionButton")
            .Pseudo(ContainerButton.StylePseudoClassDisabled)
            .Prop(Control.StylePropertyModulateSelf, Color.White);

        Builder
            .Element<Button>()
            .Class("ContractorExecutionButton")
            .Pseudo(ContainerButton.StylePseudoClassNormal)
            .Prop(Control.StylePropertyModulateSelf, Color.White);

        Builder
            .Element<Button>()
            .Class("ContractorExecutionButton")
            .Pseudo(ContainerButton.StylePseudoClassPressed)
            .Prop(ContainerButton.StylePropertyStyleBox,
                CreateStyleBox("/Textures/SS220/Interface/Contractor/contractor-pda-button-normal.png",
                    marginLeft: 2f,
                    marginRight: 2f,
                    marginTop: 2f,
                    marginBottom: 2f))
            .Prop(Control.StylePropertyModulateSelf, Color.White);


        Builder
            .Element<PanelContainer>()
            .Class("ContractorExecutionImage")
            .Prop(PanelContainer.StylePropertyPanel, StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Contractor/contractor-pda-execution-image.png")));

        Builder
            .Element<PanelContainer>()
            .Class("ContractorPhotoImage")
            .Prop(PanelContainer.StylePropertyPanel, StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Contractor/contractor-pda-photo-image.png")));

        Builder
            .Element<PanelContainer>()
            .Class("ContractAcceptedBorder")
            .Prop(PanelContainer.StylePropertyPanel,
                new StyleBoxFlat
                {
                    BorderColor = Color.FromHex("#43ff4a"),
                    BorderThickness = new Thickness(2),
                    BackgroundColor = Color.FromHex("#93abb2"),
                });
    }

}
