using Content.Client.SS220.StyleTools;
using Content.Client.SS220.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Contractor.UI;

public sealed class ContractorStyle : QuickStyle
{
    protected override void CreateRules()
    {
        Builder
            .Element<PanelContainer>()
            .Class("ContractorPanel")
            .Prop(PanelContainer.StylePropertyPanel,
                StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Contractor/background-uplink-contractor.png")));

        Builder
            .Element<PanelContainer>()
            .Class("ContractorFontPanel")
            .Prop(PanelContainer.StylePropertyPanel,
                StrechedStyleBoxTexture(Tex("/Textures/SS220/Interface/Contractor/font-uplink-contractor.png")));

        Builder
            .Element<SpriteButton>()
            .Class("ContractorContractsButton")
            .Prop(SpriteButton.StylePropertySprite,
                Sprite("/Textures/SS220/Interface/Contractor/buttons.rsi", "contracts"));

        Builder
            .Element<SpriteButton>()
            .Class("ContractorHubButton")
            .Prop(SpriteButton.StylePropertySprite,
                Sprite("/Textures/SS220/Interface/Contractor/button-hub.rsi", "hub"));

        Builder
            .Element<SpriteButton>()
            .Class("ContractorWithdrawButton")
            .Prop(SpriteButton.StylePropertySprite,
                Sprite("/Textures/SS220/Interface/Contractor/button-withdraw.rsi", "withdraw"));

        Builder
            .Element<SpriteButton>()
            .Class("ContractorExecutionButton")
            .Prop(SpriteButton.StylePropertySprite,
                Sprite("/Textures/SS220/Interface/Contractor/button-execution.rsi", "execution"));

        Builder
            .Element<SpriteButton>()
            .Class("ContractorPhotoButton")
            .Prop(SpriteButton.StylePropertySprite,
                Sprite("/Textures/SS220/Interface/Contractor/button-photo.rsi", "photo"));
    }

}
