// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Eui;
using Content.Shared.SS220.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.SS220.Administration.UI.ManageDelivery;

[UsedImplicitly]
public sealed class ManageDeliveryEui : BaseEui
{
    private readonly ManageDeliveryWindow _window;

    public ManageDeliveryEui()
    {
        _window = new ManageDeliveryWindow();
        _window.OnApplyReward += reward => SendMessage(new SetDeliveryRewardMessage(reward));
        _window.OnApplyRecipient += recordId => SendMessage(new SetDeliveryRecipientMessage(recordId));
        _window.OnApplyTimer += seconds => SendMessage(new SetDeliveryTimerMessage(seconds));
        _window.OnReplaceContents += protoId => SendMessage(new ReplaceDeliveryContentsMessage(protoId));
        _window.OnClearContents += () => SendMessage(new ClearDeliveryContentsMessage());
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is ManageDeliveryEuiState s)
            _window.UpdateState(s);
    }
}

