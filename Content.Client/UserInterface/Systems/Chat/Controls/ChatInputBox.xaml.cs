using Content.Client.Stylesheets;
using Content.Client.SS220.UserInterface.System.Chat.Controls;
using Content.Shared.Chat;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controls;
using Content.Client.SS220.UserInterface.System.Chat.Controls.LanguageSettings;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

[Virtual]
public class ChatInputBox : PanelContainer
{
    public readonly ChannelSelectorButton ChannelSelector;
    public readonly HistoryLineEdit Input;
    public readonly ChannelFilterButton FilterButton;
    public readonly HighlightButton HighlightButton; //ss220 highlight words
    public readonly LanguageSettingsButton LanguageSettings; // SS220 languages
    protected readonly BoxContainer Container;
    protected ChatChannel ActiveChannel { get; private set; } = ChatChannel.Local;

    public ChatInputBox()
    {
        Container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4
        };
        AddChild(Container);

        ChannelSelector = new ChannelSelectorButton
        {
            Name = "ChannelSelector",
            ToggleMode = true,
            StyleClasses = {"chatSelectorOptionButton"},
            MinWidth = 75
        };
        Container.AddChild(ChannelSelector);
        Input = new HistoryLineEdit
        {
            Name = "Input",
            PlaceHolder = GetChatboxInfoPlaceholder(),
            HorizontalExpand = true,
            StyleClasses = {"chatLineEdit"}
        };
        Container.AddChild(Input);
        FilterButton = new ChannelFilterButton
        {
            Name = "FilterButton",
            StyleClasses = {"chatFilterOptionButton"}
        };
        //ss220 highlight words start
        HighlightButton = new HighlightButton()
        {
            Name = "HighlightButton",
            StyleClasses = {"chatFilterOptionButton"}
        };
        //ss220 highlight words end
        // SS220 languages begin
        LanguageSettings = new LanguageSettingsButton()
        {
            Name = "LanguageSettings",
            StyleClasses = { "chatFilterOptionButton" }
        };
        // SS220 languages end
        Container.AddChild(FilterButton);
        Container.AddChild(HighlightButton); //ss220 highlight words
        Container.AddChild(LanguageSettings); // SS220 languages
        AddStyleClass(StyleNano.StyleClassChatSubPanel);
        ChannelSelector.OnChannelSelect += UpdateActiveChannel;
    }

    private void UpdateActiveChannel(ChatSelectChannel selectedChannel)
    {
        ActiveChannel = (ChatChannel) selectedChannel;
    }

    private static string GetChatboxInfoPlaceholder()
    {
        return (BoundKeyHelper.IsBound(ContentKeyFunctions.FocusChat), BoundKeyHelper.IsBound(ContentKeyFunctions.CycleChatChannelForward)) switch
        {
            (true, true) => Loc.GetString("hud-chatbox-info", ("talk-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.FocusChat)), ("cycle-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.CycleChatChannelForward))),
            (true, false) => Loc.GetString("hud-chatbox-info-talk", ("talk-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.FocusChat))),
            (false, true) => Loc.GetString("hud-chatbox-info-cycle", ("cycle-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.CycleChatChannelForward))),
            (false, false) => Loc.GetString("hud-chatbox-info-unbound")
        };
    }
}
