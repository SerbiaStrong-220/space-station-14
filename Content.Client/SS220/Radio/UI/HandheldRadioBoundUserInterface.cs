// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Radio;
using Content.Shared.SS220.Radio.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;

namespace Content.Client.SS220.Radio.UI;

[UsedImplicitly]
public sealed class HandheldRadioBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HandheldRadioMenu? _menu;

    [Dependency] private readonly IPrototypeManager _prototype = default!;

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
        SetChannelBorders(_menu);

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

        if (_menu == null || state is not HandheldRadioBoundUIState msg)
            return;

        _menu.Update(msg);
        _menu.SetChannel(_prototype.Index<RadioChannelPrototype>(msg.SelectedChannel).Frequency);
    }

    private void SetChannelBorders(HandheldRadioMenu? _menu)
    {
        if (_menu is null)
            return;

        if (!EntMan.TryGetComponent<HandheldRadioComponent>(Owner, out var handheldRadio))
            return;

        _menu.Channel.IsValid = n => (n >= handheldRadio.LowerFrequencyBorder) && (n <= handheldRadio.UpperFrequencyBorder);//set borders for UI from component

        /*
        if (_menu.Channel.Value == 0)
            _menu.Channel.Value = _prototype.Index<RadioChannelPrototype>(String.Format("Handheld{0}", handheldRadio.LowerFrequencyBorder % 1390)).Frequency;
        */
    }
}
