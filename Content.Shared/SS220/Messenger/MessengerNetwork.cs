// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Messenger;

/// <summary>
/// Commands of the device network packets between messenger cartridges and messenger servers.
/// </summary>
public enum MessengerNetworkCommand
{
    // cartridge to server
    CheckServer,
    StateUpdate,
    MessageSend,
    MarkRead,
    SetNameLock,
    SetChatMute,

    // server to cartridge
    Info,
    Contact,
    ClientContact,
    Chat,
    Messages,
    NewMessage,
}

/// <summary>
/// Keys of the values in the messenger device network packets.
/// </summary>
public enum MessengerNetworkKey
{
    Command,

    // cartridge to server
    DeviceUid,
    ChatId,
    MessageText,
    ReplyToId,
    CurrentChatIds,
    ContactsIds,
    MessagesIds,
    NameLocked,
    ChatMuted,

    // server to cartridge
    ServerName,
    Chat,
    ChatList,
    Message,
    MessageList,
    Contact,
    ContactList,

    /// <summary>
    /// The receiver must not play the ringtone.
    /// </summary>
    Silent,
}
