using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.DiscordLink;
using JetBrains.Annotations;

namespace Content.Client.SS220.UserInterface.DiscordLink;

[UsedImplicitly]
public sealed class DiscordLinkEui : BaseEui
{
    private DiscordLinkWindow DiscordWindow { get; }

    public DiscordLinkEui()
    {
        DiscordWindow = new DiscordLinkWindow();
        DiscordWindow.OnClose += DiscordWindow_OnClose;
    }

    private void DiscordWindow_OnClose()
    {
        SendMessage(new CloseEuiMessage());
    }

    public override void Closed()
    {
        base.Closed();
        DiscordWindow.Close();
    }

    public override void Opened()
    {
        DiscordWindow.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not DiscordLinkEuiState discordLink)
        {
            return;
        }

        DiscordWindow.SetLink(discordLink.LinkKey);
    }
}
