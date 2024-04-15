// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Radio;
using Content.Shared.SS220.Radio.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Radio.UI;

[UsedImplicitly]
public sealed class HandheldRadioBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HandheldRadioMenu? _menu;

    public HandheldRadioBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnMicPressed += enabled =>
        {
            SendMessage(new ToggleHandheldRadioMicMessage(enabled));
        };
        _menu.OnSpeakerPressed += enabled =>
        {
            SendMessage(new ToggleHandheldRadioSpeakerMessage(enabled));
        };
        _menu.OnChannelSelected += channel =>
        {
            SendMessage(new SelectHandheldRadioChannelMessage(channel));
        };

        if (EntMan.TryGetComponent<HandheldRadioComponent>(Owner, out var handheldRadio))
        {
            _menu.Channel.IsValid = n => (n >= handheldRadio.LowerFrequencyBorder) && (n <= handheldRadio.UpperFrequencyBorder);
        }

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not HandheldRadioBoundUIState msg)
            return;

        _menu?.Update(msg);
    }
}
