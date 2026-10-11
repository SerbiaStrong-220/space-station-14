// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Messenger;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Messenger;

/// <summary>
/// Data of a messenger server. All logic lives in <see cref="MessengerServerSystem"/>.
/// All ids are unique within one server.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class MessengerServerComponent : Component
{
    [DataField("serverName")]
    public string Name = "";

    /// <summary>
    /// How often contact names are synchronized with id cards.
    /// </summary>
    [DataField]
    public TimeSpan ContactUpdateInterval = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextContactUpdate;

    /// <summary>
    /// Id that will be given to the next created contact, chat or message.
    /// </summary>
    public uint NextId;

    /// <summary>
    /// Authentication entity (id card) to contact id.
    /// </summary>
    public readonly Dictionary<EntityUid, uint> ClientToContact = new();

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
}
