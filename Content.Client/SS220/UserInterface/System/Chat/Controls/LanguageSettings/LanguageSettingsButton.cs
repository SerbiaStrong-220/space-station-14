// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using System.Numerics;

namespace Content.Client.SS220.UserInterface.System.Chat.Controls.LanguageSettings;

public sealed class LanguageSettingsButton : ChatPopupButton<LanguageSettingsPopup>
{
    public LanguageSettingsButton()
    {
        IoCManager.InjectDependencies(this);

        var texture = IoCManager.Resolve<IResourceCache>()
            .GetTexture("/Textures/SS220/Interface/Nano/language_settings-button.png");

        AddChild(new TextureRect
        {
            Texture = texture,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        });

        OnPressed += _ => Popup.Update();
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalPos = GlobalPosition;
        var (minX, minY) = Popup.MinSize;
        return UIBox2.FromDimensions(
            globalPos,
            new Vector2(Math.Max(minX, Popup.MinWidth), minY));
    }
}
