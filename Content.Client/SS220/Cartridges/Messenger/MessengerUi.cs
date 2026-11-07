// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Cartridges.Messenger;

public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiState? _messengerUiState;
    private string? _errorText;

    private enum View
    {
        ChatsList,
        ChatHistory,
    }

    private View _currentView;

    private MessengerUiFragment? _fragment;
    private BoundUserInterface? _userInterface;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _userInterface = userInterface;
        _currentView = View.ChatsList;
        _fragment = new MessengerUiFragment();
        _fragment.ChatsSearch.OnTextChanged += args =>
        {
            _fragment.SearchString = args.Text == "" ? null : args.Text;
            UpdateUiState();
        };

        _fragment.OnHistoryViewPressed += chatId =>
        {
            _fragment.CurrentChat = chatId;
            _currentView = View.ChatHistory;
            _fragment.SearchString = null;
            _fragment.ChatsSearch.Clear();

            MarkChatRead(chatId);
            UpdateUiState();
            ValidateState(userInterface);
        };

        _fragment.OnMessageSendButtonPressed += (chatId, text, replyToId) =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new MessengerSendMessageUiEvent(chatId, text, replyToId)));
        };

        _fragment.OnBackButtonPressed += () =>
        {
            _fragment.SearchString = null;
            _fragment.ChatsSearch.Clear();
            _currentView = View.ChatsList;

            UpdateUiState();
        };

        _fragment.OnNameLockToggled += locked =>
        {
            if (_messengerUiState != null)
                _messengerUiState.NameLocked = locked;

            userInterface.SendMessage(new CartridgeUiMessage(new MessengerSetNameLockUiEvent(locked)));
        };

        _fragment.OnRefreshPressed += () =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new MessengerUpdateStateUiEvent(true)));
        };

        if (_messengerUiState == null)
        {
            userInterface.SendMessage(new CartridgeUiMessage(new MessengerUpdateStateUiEvent(true)));
            return;
        }

        UpdateUiState();
        ValidateState(userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        _errorText = null;

        switch (state)
        {
            case MessengerUiState messengerUiState:
                _messengerUiState = messengerUiState;
                break;

            case MessengerClientContactUiState contactUiState:
                if (_messengerUiState == null)
                    break;

                _messengerUiState.ClientContact = contactUiState.ClientContact;
                break;

            case MessengerContactUiState messengerContactUiState:
                if (_messengerUiState == null)
                    break;

                foreach (var messengerContact in messengerContactUiState.Contacts)
                {
                    _messengerUiState.Contacts[messengerContact.Id] = messengerContact;
                }
                break;

            case MessengerMessagesUiState messengerMessagesUiState:
                if (_messengerUiState == null)
                    break;

                foreach (var messengerMessage in messengerMessagesUiState.Messages)
                {
                    _messengerUiState.Messages[messengerMessage.Id] = messengerMessage;

                    var chat = GetOrCreateChat(messengerMessage.ChatId);

                    chat.Messages.Add(messengerMessage.Id);
                    chat.LastMessage = chat.Messages.Max();
                }
                break;

            case MessengerNewChatMessageUiState st:
                OnNewMessage(st);
                break;

            case MessengerChatUpdateUiState st:
                if (_messengerUiState == null)
                    break;

                foreach (var messengerChat in st.Chats)
                {
                    if (_messengerUiState.Chats.TryGetValue(messengerChat.Id, out var chatUi))
                    {
                        chatUi.Name = messengerChat.Name ?? chatUi.Name;
                        chatUi.Members.UnionWith(messengerChat.MembersId);
                        chatUi.LastMessage = messengerChat.LastMessageId ?? chatUi.LastMessage;
                        chatUi.Messages.UnionWith(messengerChat.MessagesId);
                        continue;
                    }

                    _messengerUiState.Chats.Add(messengerChat.Id,
                        new MessengerChatUiState(messengerChat.Id, messengerChat.Name, messengerChat.Kind,
                            messengerChat.MembersId, messengerChat.MessagesId, messengerChat.LastMessageId,
                            _messengerUiState.Chats.Count));
                }

                break;

            case MessengerErrorUiState errorUiState:
                _errorText = Loc.GetString(errorUiState.LocId);
                break;
        }

        UpdateUiState();
    }

    private void OnNewMessage(MessengerNewChatMessageUiState state)
    {
        if (_messengerUiState == null)
            return;

        _messengerUiState.Messages.TryAdd(state.Message.Id, state.Message);

        var chat = GetOrCreateChat(state.ChatId);
        var isNew = chat.Messages.Add(state.Message.Id);

        chat.LastMessage = state.Message.Id;

        if (!isNew || state.Message.FromContactId == _messengerUiState.ClientContact.Id)
            return;

        // when the chat is on the screen, the message is read at once
        if (_currentView == View.ChatHistory && _fragment?.CurrentChat == state.ChatId)
            MarkChatRead(state.ChatId);
        else
            chat.Unread++;
    }

    private void MarkChatRead(ChatUid chatId)
    {
        if (_messengerUiState == null || !_messengerUiState.Chats.TryGetValue(chatId, out var chat))
            return;

        chat.Unread = 0;
        _userInterface?.SendMessage(new CartridgeUiMessage(new MessengerMarkReadUiEvent(chatId)));
    }

    private MessengerChatUiState GetOrCreateChat(ChatUid chatId)
    {
        if (_messengerUiState == null)
        {
            return new MessengerChatUiState(chatId, null, MessengerChatKind.Contact, new HashSet<ContactUid>(),
                new HashSet<MessageUid>(), null, 0);
        }

        if (!_messengerUiState.Chats.TryGetValue(chatId, out var chat))
        {
            chat = new MessengerChatUiState(chatId, null, MessengerChatKind.Contact, new HashSet<ContactUid>(),
                new HashSet<MessageUid>(), null, _messengerUiState.Chats.Count);

            chat.ForceUpdate = true;

            _messengerUiState.Chats.Add(chatId, chat);

            return chat;
        }

        return chat;
    }

    private void ValidateState(BoundUserInterface userInterface)
    {
        var receivedContacts = new HashSet<ContactUid>();
        var receivedMessages = new HashSet<MessageUid>();
        var receivedChats = new HashSet<ChatUid>();

        if (_messengerUiState == null)
            return;

        foreach (var (chatId, chat) in _messengerUiState.Chats)
        {
            if (!chat.ForceUpdate)
            {
                receivedChats.Add(chatId);
            }

            chat.ForceUpdate = false;
        }

        foreach (var (contactId, _) in _messengerUiState.Contacts)
        {
            receivedContacts.Add(contactId);
        }

        foreach (var (messageId, _) in _messengerUiState.Messages)
        {
            receivedMessages.Add(messageId);
        }

        userInterface.SendMessage(
            new CartridgeUiMessage(new MessengerUpdateStateUiEvent(receivedContacts, receivedMessages, receivedChats)));
    }

    private void UpdateUiState()
    {
        switch (_currentView)
        {
            case View.ChatsList:
                if (_messengerUiState != null)
                    _fragment?.UpdateChatsState(_messengerUiState);
                break;

            case View.ChatHistory:
                if (_messengerUiState != null)
                    _fragment?.UpdateChatHistoryState(_messengerUiState);
                break;
        }

        if (_errorText != null)
            _fragment?.DisplayError(_errorText);
    }
}
