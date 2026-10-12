// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Access;
using Content.Shared.SS220.Messenger;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Data of a messenger server. All ids are unique within one server.
/// Contacts of the clients are stored in <see cref="MessengerClientComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class MessengerServerComponent : Component
{
    [DataField("serverName")]
    public string Name = "";

    // NOTE: we don't have special prototype id for that
    [DataField]
    public HashSet<string> AiReadAlertLevels = ["red", "gamma", "delta", "epsilon"];

    /// <summary>
    /// If true, the station AI of the server map gets its own contact and can write to the crew.
    /// </summary>
    [DataField]
    public bool AiContactEnabled;

    /// <summary>
    /// Group chats of departments: access level that gives the membership to the name of the chat.
    /// The chats are created once, members are added and removed by the access of their id cards.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AccessLevelPrototype>, LocId> DepartmentChats = new();

    /// <summary>
    /// Chat id of a department chat to the access level of its members.
    /// Chats added in runtime
    /// </summary>
    public readonly Dictionary<ChatUid, ProtoId<AccessLevelPrototype>> DepartmentChatAccess = new();

    /// <summary>
    /// Contact id of the station AI to the AI entity. These contacts have no net addresses,
    /// the AI gets the updates through its ui.
    /// </summary>
    public readonly Dictionary<ContactUid, EntityUid> AiContacts = new();

    /// <summary>
    /// Id that will be given to the next created contact, chat or message.
    /// </summary>
    public uint NextId;

    public readonly Dictionary<ContactUid, MessengerContact> Contacts = new();

    public readonly Dictionary<ChatUid, MessengerChat> Chats = new();

    public readonly Dictionary<MessageUid, MessengerMessage> Messages = new();

    /// <summary>
    /// Chats available to every contact of the server.
    /// </summary>
    public readonly HashSet<ChatUid> PublicChats = new();

    /// <summary>
    /// Contact id to chats available only to this contact (invited).
    /// </summary>
    public readonly Dictionary<ContactUid, HashSet<ChatUid>> PrivateChats = new();

    /// <summary>
    /// Contact id to (chat id to the last message the contact has read in it).
    /// </summary>
    public readonly Dictionary<ContactUid, Dictionary<ChatUid, MessageUid>> LastReadMessages = new();
}
