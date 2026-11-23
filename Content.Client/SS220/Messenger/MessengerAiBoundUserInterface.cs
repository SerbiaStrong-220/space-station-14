// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Messenger;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Messenger;

public sealed class MessengerAiBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private MessengerAiWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MessengerAiWindow>();
        _window.OnRequest += chatId => SendMessage(new MessengerAiRequestMessage(chatId));

        SendMessage(new MessengerAiRequestMessage(null));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is MessengerAiViewMessage view)
            _window?.SetView(view);
    }
}
