// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.CartridgeLoader.Cartridges.Messenger;
using System.Numerics;
using Content.Shared.Messenger;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Messenger;

/// <summary>
/// The messenger window of the station AI. It shows the same interface as the cartridge does,
/// the states come wrapped into <see cref="MessengerAiStateMessage"/>.
/// </summary>
public sealed class MessengerAiClientBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly MessengerUi _messengerUi = new();

    protected override void Open()
    {
        base.Open();

        var window = this.CreateWindow<DefaultWindow>();
        window.Title = Loc.GetString("messenger-ai-client-window-title");
        window.MinSize = new Vector2(420, 520);

        _messengerUi.Setup(this, Owner);
        window.Contents.AddChild(_messengerUi.GetUIFragmentRoot());
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is MessengerAiStateMessage stateMessage)
            _messengerUi.UpdateState(stateMessage.State);
    }
}
