using Content.Shared.SS220.Radio;
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