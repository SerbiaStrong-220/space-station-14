// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Cartridges.Messenger;

[UsedImplicitly]
public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiState? _messengerUiState;
    private string? _errorText;

    private const int ChatsList = 0;
    private const int ChatHistory = 1;

    private int _currentView;

    private MessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _currentView = ChatsList;
        _fragment = new MessengerUiFragment();

        _fragment.ChatsSearch.OnTextChanged += args =>
        {
            _fragment.SearchString = string.IsNullOrWhiteSpace(args.Text) ? null : args.Text.ToLower();
            UpdateUiState();
        };

        _fragment.OnHistoryViewPressed += chatId =>
        {
            _fragment.CurrentChat = chatId;
            _currentView = ChatHistory;
            _fragment.SearchString = null;

            UpdateUiState();
            ValidateState(userInterface);
        };

        _fragment.OnMessageSendButtonPressed += (chatId, text) =>
        {
            var message = new MessengerSendMessageUiEvent(chatId, text);
            userInterface.SendMessage(new CartridgeUiMessage(message));
        };

        _fragment.OnClearChatPressed += (chatId, deleteAll) =>
        {
            var message = new MessengerClearChatUiMessageEvent(chatId, deleteAll);
            userInterface.SendMessage(new CartridgeUiMessage(message));
        };

        _fragment.OnBackButtonPressed += _ =>
        {
            _fragment.SearchString = null;

            if (_currentView != ChatsList)
                _currentView--;

            if (_currentView == ChatsList && _messengerUiState != null)
                _fragment.UpdateChatsState(_messengerUiState);
        };

        if (_messengerUiState == null)
        {
            var message = new MessengerUpdateStateUiEvent(true);
            userInterface.SendMessage(new CartridgeUiMessage(message));
            return;
        }

        UpdateUiState();
        ValidateState(userInterface);
    }

    private void DefaultState(MessengerUiState state)
    {
        _messengerUiState ??= state;
        UpdateUiState();
    }

    private void ClientContactState(MessengerClientContactUiState state)
    {
        _messengerUiState!.ClientContact = state.ClientContact;
    }

    private void ContactUiState(MessengerContactUiState state)
    {
        foreach (var messengerContact in state.Contacts)
        {
            _messengerUiState!.Contacts[messengerContact.Id] = messengerContact;
        }
    }

    private void MessagesUiState(MessengerMessagesUiState state)
    {
        foreach (var messengerMessage in state.Messages)
        {
            _messengerUiState!.Messages[messengerMessage.Id] = messengerMessage;

            var chat = GetOrCreateChat(messengerMessage.ChatId);
            chat.Messages.Add(messengerMessage.Id);
            chat.LastMessage = chat.Messages.Max();
        }
    }

    private void NewChatMessageState(MessengerNewChatMessageUiState state)
    {
        _messengerUiState!.Messages.TryAdd(state.Message.Id, state.Message);

        var chat = GetOrCreateChat(state.ChatId);

        chat.Messages.Add(state.Message.Id);
        chat.LastMessage = state.Message.Id;
        MoveChatToTopExceptFavorites(chat.Id);
    }

    private void DeleteMsgInChatState(MessengerDeleteMsgInChatUiState state)
    {
        if (state.ChatId == null)
            return;

        var chat = GetOrCreateChat(state.ChatId.Value);
        chat.Messages.Clear();
        chat.LastMessage = null;
    }

    private void ChatUpdateState(MessengerChatUpdateUiState state)
    {
        foreach (var messengerChat in state.Chats)
        {
            if (_messengerUiState!.Chats.TryGetValue(messengerChat.Id, out var chatUi))
            {
                chatUi.Name = messengerChat.Name ?? chatUi.Name;
                chatUi.Members.UnionWith(messengerChat.MembersId);
                chatUi.LastMessage = messengerChat.LastMessageId ?? chatUi.LastMessage;
                chatUi.Messages.UnionWith(messengerChat.MessagesId);
                chatUi.NewMessages = true;
                continue;
            }

            var newState = new MessengerChatUiState(
                messengerChat.Id,
                messengerChat.Name,
                messengerChat.Kind,
                messengerChat.MembersId,
                messengerChat.MessagesId,
                messengerChat.LastMessageId,
                _messengerUiState.Chats.Count);

            _messengerUiState.Chats.Add(messengerChat.Id, newState);
        }
    }

    private void ErrorUiState(MessengerErrorUiState state)
    {
        _errorText = state.Text;
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        _errorText = null;

        switch (state)
        {
            case MessengerUiState uiState:
                DefaultState(uiState);
                return;
            case MessengerErrorUiState errorUiState:
                ErrorUiState(errorUiState);
                UpdateUiState();
                return;
        }

        if (_messengerUiState == null)
            return;

        switch (state)
        {
            case MessengerClientContactUiState clientContactUiState:
                ClientContactState(clientContactUiState);
                break;

            case MessengerContactUiState contactUiState:
                ContactUiState(contactUiState);
                break;

            case MessengerMessagesUiState messagesUiState:
                MessagesUiState(messagesUiState);
                break;

            case MessengerNewChatMessageUiState newChatMessageUiState:
                NewChatMessageState(newChatMessageUiState);
                break;

            case MessengerDeleteMsgInChatUiState deleteMsgInChatUiState:
                DeleteMsgInChatState(deleteMsgInChatUiState);
                break;

            case MessengerChatUpdateUiState chatUpdateUiState:
                ChatUpdateState(chatUpdateUiState);
                break;
        }

        UpdateUiState();
    }

    private MessengerChatUiState GetOrCreateChat(uint chatId)
    {
        if (_messengerUiState == null)
        {
            return new MessengerChatUiState(
                chatId,
                null,
                MessengerChatKind.Contact,
                [],
                [],
                null,
                0);
        }

        if (_messengerUiState.Chats.TryGetValue(chatId, out var chat))
            return chat;

        chat = new MessengerChatUiState(
            chatId,
            null,
            MessengerChatKind.Contact,
            [],
            [],
            null,
            _messengerUiState.Chats.Count)
        {
            ForceUpdate = true,
        };

        _messengerUiState.Chats.Add(chatId, chat);

        return chat;
    }

    private void ValidateState(BoundUserInterface userInterface)
    {
        if (_messengerUiState == null)
            return;

        var receivedContacts = new HashSet<uint>();
        var receivedMessages = new HashSet<uint>();
        var receivedChats = new HashSet<uint>();

        foreach (var (chatId, chat) in _messengerUiState.Chats)
        {
            if (!chat.ForceUpdate)
                receivedChats.Add(chatId);

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
        if (_messengerUiState != null)
        {
            switch (_currentView)
            {
                case ChatsList:
                    _fragment?.UpdateChatsState(_messengerUiState);
                    break;

                case ChatHistory:
                    _fragment?.UpdateChatHistoryState(_messengerUiState);
                    break;
            }
        }

        if (_errorText != null)
            _fragment?.DisplayError(_errorText);
    }

    private void MoveChatToTopExceptFavorites(uint chatId)
    {
        if (_messengerUiState == null)
            return;

        var chats = _messengerUiState.Chats;

        if (!chats.TryGetValue(chatId, out var chatToMove))
            return;

        var favoriteId = _messengerUiState.Chats
            .Where(pair => pair.Value.SortNumber == 0)
            .Select(pair => pair.Key)
            .FirstOrDefault();

        if (chatToMove.Id == favoriteId)
            return;

        foreach (var chat in chats.Values)
        {
            if (chat.Id != favoriteId && chat.Id != chatId)
                chat.SortNumber++;
        }

        chatToMove.SortNumber = 1;
    }
}
