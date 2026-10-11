// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Messenger;

namespace Content.Server.Messenger;

/// <summary>
/// Data of a messenger server. All logic lives in <see cref="MessengerServerSystem"/>.
/// All ids are unique within one server.
/// </summary>
[RegisterComponent]
public sealed partial class MessengerServerComponent : Component
{
    [DataField("serverName")]
    public string Name = "";

    /// <summary>
    /// Station alert levels at which the station AI is allowed to read chats of this server.
    /// </summary>
    [DataField]
    public HashSet<string> AiReadAlertLevels = new() { "red", "gamma", "delta", "epsilon" };

    /// <summary>
    /// Id that will be given to the next created contact, chat or message.
    /// </summary>
    public uint NextId;

    /// <summary>
    /// Authentication entity (id card) to contact id.
    /// </summary>
    public readonly Dictionary<EntityUid, uint> ClientToContact = new();

    /// <summary>
    /// Id cards that use the contact of another card (e.g. the card of a paradox clone).
    /// Such a card does not set the name of the contact, and the contact is online if any of its cards is.
    /// </summary>
    public readonly HashSet<EntityUid> LinkedCards = new();

    public readonly Dictionary<uint, MessengerContact> Contacts = new();

    public readonly Dictionary<uint, MessengerChat> Chats = new();

    public readonly Dictionary<uint, MessengerMessage> Messages = new();

    /// <summary>
    /// Chats available to every contact of the server.
    /// </summary>
    public readonly HashSet<uint> PublicChats = new();

    /// <summary>
    /// Contact id to chats available only to this contact (invited).
    /// </summary>
    public readonly Dictionary<uint, HashSet<uint>> PrivateChats = new();

    /// <summary>
    /// Contact id to (chat id to the last message the contact has read in it).
    /// </summary>
    public readonly Dictionary<uint, Dictionary<uint, uint>> LastReadMessages = new();
}
