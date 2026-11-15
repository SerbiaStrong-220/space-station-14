// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Messenger;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Messenger account of a client: an id card or the station AI.
/// The id card is what identifies a person for the messenger servers
/// </summary>
[RegisterComponent]
public sealed partial class MessengerClientComponent : Component
{
    /// <summary>
    /// Messenger server to the contact of this client on it.
    /// </summary>
    public readonly Dictionary<EntityUid, ContactUid> Contacts = new();

    /// <summary>
    /// If true, the contacts belong to another client (e.g. the original of a paradox clone), so this client
    /// writes from the same accounts. A linked client does not set names of the contacts and can't be removed
    /// from the chats of the departments, because it does not have to repeat the access of the original.
    /// </summary>
    public bool IsLinked;

    /// <summary>
    /// If true, a linked agent id card keeps its account when it is inserted to another pda.
    /// </summary>
    public bool RebindLocked;
}
