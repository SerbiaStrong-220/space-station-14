using System.Numerics;
using Content.Shared.Chat;
using Content.Shared.FixedPoint;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorButton : ChatPopupButton<ChannelSelectorPopup>
{
    public event Action<ChatSelectChannel>? OnChannelSelect;

    public ChatSelectChannel SelectedChannel { get; private set; }

    private const int SelectorDropdownOffset = 38;

    public ChannelSelectorButton()
    {
        Name = "ChannelSelector";

        Popup.Selected += OnChannelSelected;

        if (Popup.FirstChannel is { } firstSelector)
        {
            Select(firstSelector);
        }
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalLeft = GlobalPosition.X;
        var globalBot = GlobalPosition.Y + Height;
        return UIBox2.FromDimensions(
            new Vector2(globalLeft, globalBot),
            new Vector2(SizeBox.Width, SelectorDropdownOffset));
    }

    private void OnChannelSelected(ChatSelectChannel channel)
    {
        Select(channel);
    }

    public void Select(ChatSelectChannel channel)
    {
        if (Popup.Visible)
        {
            Popup.Close();
        }

        if (SelectedChannel == channel)
            return;
        SelectedChannel = channel;
        OnChannelSelect?.Invoke(channel);
    }

    public static string ChannelSelectorName(ChatSelectChannel channel)
    {
        return Loc.GetString($"hud-chatbox-select-channel-{channel}");
    }

    public Color ChannelSelectColor(ChatSelectChannel channel)
    {
        return channel switch
        {
            ChatSelectChannel.Radio => Color.LimeGreen,
            ChatSelectChannel.LOOC => Color.MediumTurquoise,
            ChatSelectChannel.OOC => Color.LightSkyBlue,
            ChatSelectChannel.Dead => Color.MediumPurple,
            ChatSelectChannel.Admin => Color.HotPink,
            ChatSelectChannel.Telepathy => Color.BetterViolet, //ss220 telepathy
            _ => Color.DarkGray
        };
    }

    public void UpdateChannelSelectButton(ChatSelectChannel channel, Shared.Radio.RadioChannelPrototype? radio, FixedPoint2? frequency = null /*SS220-add-frequency-radio */)
    {
        // SS220-add-frequency-radio-begin
        string channelName;
        if (radio is not null)
        {
            channelName = radio.FrequencyRadio && frequency is not null
                ? Loc.GetString(radio.FrequencyChanelName, ("freq", frequency.Value.Float()))
                : Loc.GetString(radio.Name);
        }
        else
        {
            channelName = ChannelSelectorName(channel);
        }

        Text = channelName;
        // Text = radio != null ? Loc.GetString(radio.Name) : ChannelSelectorName(channel); // [wizden-code] SS220-add-frequency-radio
        // SS220-add-frequency-radio-end
        Modulate = radio?.Color ?? ChannelSelectColor(channel);
    }
}
