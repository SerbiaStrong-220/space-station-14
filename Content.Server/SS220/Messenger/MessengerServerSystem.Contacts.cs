// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.DeviceNetwork;
using Content.Shared.PDA;
using Content.Shared.SS220.Messenger;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Map;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Names and statuses of the contacts.
/// </summary>
public sealed partial class MessengerServerSystem
{
    private void InitializeContacts()
    {
        SubscribeLocalEvent<IdCardComponent, EntityTerminatingEvent>(OnIdCardTerminating);

        SubscribeLocalEvent<CryostorageContainedComponent, ComponentStartup>(OnCryostorageStartup);
        SubscribeLocalEvent<CryostorageContainedComponent, ComponentRemove>(OnCryostorageRemove);

        SubscribeLocalEvent<MessengerServerComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnIdCardTerminating(Entity<IdCardComponent> ent, ref EntityTerminatingEvent args)
    {
        UpdateAllContactStatuses();
    }

    private void OnCryostorageStartup(Entity<CryostorageContainedComponent> ent, ref ComponentStartup args)
    {
        UpdateAllContactStatuses();
    }

    private void OnCryostorageRemove(Entity<CryostorageContainedComponent> ent, ref ComponentRemove args)
    {
        UpdateAllContactStatuses(ent);
    }

    /// <summary>
    /// A grid that jumped to another map takes everyone on it out of the server range.
    /// </summary>
    private void OnParentChanged(Entity<MessengerServerComponent> ent, ref EntParentChangedMessage args)
    {
        UpdateAllContactStatuses();
    }

    /// <summary>
    /// Names follow the id cards, but a card does not tell when it is renamed, so they are checked
    /// when a client asks for the state or sends a message. Changed contacts are sent to every client.
    /// </summary>
    private void UpdateContactNames(Entity<MessengerServerComponent> ent)
    {
        List<MessengerContact>? changed = null;
        var query = EntityQueryEnumerator<MessengerClientComponent>();

        while (query.MoveNext(out var clientUid, out var client))
        {
            if (client.IsLinked
                || !client.Contacts.TryGetValue(ent, out var contactId)
                || ent.Comp.NameLockedContacts.Contains(contactId)
                || !TryGetOwnerName(clientUid, out var name))
                continue;

            if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact) || contact.Name == name)
                continue;

            contact.Name = name;
            changed ??= new List<MessengerContact>();
            changed.Add(contact);
        }

        if (changed != null)
            SendContactsToAll(ent, changed);
    }

    /// <summary>
    /// A locked contact keeps its current name when the id card is renamed. Unlocking brings the name of the card back.
    /// </summary>
    private void SetNameLock(Entity<MessengerServerComponent> ent, ContactUid contactId, bool locked)
    {
        if (!ent.Comp.Contacts.ContainsKey(contactId))
            return;

        if (locked)
        {
            ent.Comp.NameLockedContacts.Add(contactId);
            return;
        }

        if (ent.Comp.NameLockedContacts.Remove(contactId))
            UpdateContactNames(ent);
    }

    private bool TryGetOwnerName(EntityUid clientUid, [NotNullWhen(true)] out string? name)
    {
        name = null;

        if (HasComp<StationAiHeldComponent>(clientUid))
            name = MetaData(clientUid).EntityName;
        else if (TryComp<IdCardComponent>(clientUid, out var card))
            name = card.FullName;

        return name != null;
    }

    private void UpdateAllContactStatuses(EntityUid? markIgnored = null)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            UpdateContactStatuses((serverUid, server), markIgnored);
        }
    }

    /// <summary>
    /// Recalculates statuses of all contacts and sends the changed ones to every client.
    /// A contact that returned to the server range also gets the server info, so its client resyncs missed messages.
    /// </summary>
    /// <param name="markIgnored">Entity whose cryostorage or AI mark is being removed and must not be counted.</param>
    private void UpdateContactStatuses(Entity<MessengerServerComponent> ent, EntityUid? markIgnored = null)
    {
        var serverMap = Transform(ent.Owner).MapID;
        var powered = IsPowered(ent);
        var statuses = new Dictionary<ContactUid, MessengerContactStatus>();
        List<MessengerContact>? changed = null;
        var query = EntityQueryEnumerator<MessengerClientComponent>();

        // a contact with several clients gets the best status of them (Online is the smallest value)
        while (query.MoveNext(out var clientUid, out var client))
        {
            if (!client.Contacts.TryGetValue(ent, out var contactId))
                continue;

            var status = GetContactStatus(clientUid, serverMap, powered, markIgnored);

            if (!statuses.TryGetValue(contactId, out var best) || status < best)
                statuses[contactId] = status;
        }

        foreach (var (contactId, status) in statuses)
        {
            if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact) || contact.Status == status)
                continue;

            contact.Status = status;
            changed ??= new List<MessengerContact>();
            changed.Add(contact);

            if (status == MessengerContactStatus.Online)
                SendToContact(ent, contactId, CreateInfoPayload(ent.Comp));
        }

        if (changed != null)
            SendContactsToAll(ent, changed);
    }

    private void SendContactsToAll(Entity<MessengerServerComponent> ent, List<MessengerContact> contacts)
    {
        var payload = new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Contact,
            [MessengerNetworkKey.ContactList.ToString()] = contacts,
        };

        foreach (var contactId in ent.Comp.Contacts.Keys)
        {
            SendToContact(ent, contactId, payload);
        }
    }

    private MessengerContactStatus GetContactStatus(EntityUid clientUid,
        MapId serverMap,
        bool serverPowered,
        EntityUid? markIgnored)
    {
        if (TerminatingOrDeleted(clientUid))
            return MessengerContactStatus.OutOfRange;

        // the AI has no pda, it is reachable while it sits in a core on the server map
        if (clientUid != markIgnored && HasComp<StationAiHeldComponent>(clientUid))
        {
            return serverPowered && Transform(clientUid).MapID == serverMap
                ? MessengerContactStatus.Online
                : MessengerContactStatus.OutOfRange;
        }

        if (IsInCryostorage(clientUid, markIgnored))
            return MessengerContactStatus.Cryo;

        // assume contact is reachable only while its id card is inside a pda on the server map
        if (!serverPowered
            || !_containerSystem.TryGetContainingContainer(clientUid, out var container)
            || container.ID != PdaComponent.PdaIdSlotId)
            return MessengerContactStatus.OutOfRange;

        return Transform(container.Owner).MapID == serverMap
            ? MessengerContactStatus.Online
            : MessengerContactStatus.OutOfRange;
    }

    /// <summary>
    /// A body that left for cryostorage keeps its items, so the outermost container owner is the body itself.
    /// </summary>
    private bool IsInCryostorage(EntityUid uid, EntityUid? ignored)
    {
        var current = uid;

        while (_containerSystem.TryGetContainingContainer(current, out var container))
        {
            current = container.Owner;
        }

        return current != ignored && HasComp<CryostorageContainedComponent>(current);
    }
}
