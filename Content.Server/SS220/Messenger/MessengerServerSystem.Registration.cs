// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.SS220.CartridgeLoader.Cartridges;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.PDA;
using Content.Shared.Power;
using Content.Shared.SS220.Messenger;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Creation of the contacts and binding of the pda addresses to them.
/// </summary>
public sealed partial class MessengerServerSystem
{
    private void InitializeRegistration()
    {
        SubscribeLocalEvent<MessengerServerComponent, MapInitEvent>(OnServerMapInit);
        SubscribeLocalEvent<MessengerServerComponent, PowerChangedEvent>(OnServerPowerChanged);

        // NOTE: Contacts are born and move only when an id card or a cartridge appears in a pda, so we listen to that.
        // The pda and cartridge side is subscribed by other systems already, so we listen from the id card side
        // NOTE2: receive the cartridge installation from MessengerClientCartridgeSystem.
        SubscribeLocalEvent<IdCardComponent, EntGotInsertedIntoContainerMessage>(OnIdCardInserted);
        SubscribeLocalEvent<IdCardComponent, EntGotRemovedFromContainerMessage>(OnIdCardRemoved);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnServerMapInit(Entity<MessengerServerComponent> ent, ref MapInitEvent args)
    {
        EnsureDepartmentChats(ent);
        RegisterAllCartridges(ent);
    }

    private void OnServerPowerChanged(Entity<MessengerServerComponent> ent, ref PowerChangedEvent args)
    {
        // the server did not see the cartridges while it was off
        if (args.Powered)
            RegisterAllCartridges(ent);
        else
            UpdateContactStatuses(ent);
    }

    private void OnIdCardInserted(Entity<IdCardComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == PdaComponent.PdaIdSlotId)
            RefreshLoader(args.Container.Owner);
    }

    private void OnIdCardRemoved(Entity<IdCardComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID == PdaComponent.PdaIdSlotId)
            RefreshLoader(args.Container.Owner);
    }

    /// <summary>
    /// Called by the cartridge system when the cartridge is installed to a loader.
    /// </summary>
    public void OnCartridgeInstalled(EntityUid cartridge, EntityUid loader)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            if (Transform(serverUid).MapID != Transform(cartridge).MapID)
                continue;

            var serverEnt = (serverUid, server);

            RefreshCartridge(serverEnt, cartridge, loader);
            UpdateContactStatuses(serverEnt);

            break;
        }
    }

    private void OnCartridgeRemoved(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNet) || string.IsNullOrEmpty(deviceNet.Address))
            return;

        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out _, out var server))
        {
            ReleaseAddress(server, deviceNet.Address);
        }
    }

    /// <summary>
    /// Rebinds the messenger cartridges of the loader, e.g. after its id card was changed.
    /// </summary>
    private void RefreshLoader(EntityUid loader)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            var serverEnt = (serverUid, server);

            foreach (var program in _cartridgeLoader.GetInstalled(loader))
            {
                RefreshProgram(serverEnt, program, loader);
            }

            if (TryComp<CartridgeLoaderComponent>(loader, out var loaderComp) && loaderComp.CartridgeSlot.Item is { } slotItem)
                RefreshProgram(serverEnt, slotItem, loader);
        }
    }

    private void RefreshProgram(Entity<MessengerServerComponent> server, EntityUid program, EntityUid loader)
    {
        if (!HasComp<MessengerClientCartridgeComponent>(program))
            return;

        RefreshCartridge(server, program, loader);
        UpdateContactStatuses(server);
    }

    /// <summary>
    /// Registers owners of all messenger cartridges that have access to the server,
    /// even if nobody has opened the cartridge. Contacts are never removed to keep the chat history.
    /// </summary>
    private void RegisterAllCartridges(Entity<MessengerServerComponent> ent)
    {
        var query = EntityQueryEnumerator<MessengerClientCartridgeComponent>();

        while (query.MoveNext(out var cartridgeUid, out var cartridge))
        {
            if (cartridge.Loader.IsValid())
                RefreshCartridge(ent, cartridgeUid, cartridge.Loader);
        }

        RegisterAllAi(ent);
        UpdateContactStatuses(ent);
    }

    /// <summary>
    /// A contact is reachable only on the cartridge that currently holds its id card. Registers the owner of the
    /// cartridge if it has access, binds the cartridge address to it and drops the address from the previous owner,
    /// otherwise the previous owner would keep receiving the messages on this pda.
    /// </summary>
    private void RefreshCartridge(Entity<MessengerServerComponent> ent, EntityUid cartridge, EntityUid loader)
    {
        if (!IsPowered(ent)
            || !TryComp<DeviceNetworkComponent>(cartridge, out var deviceNet)
            || string.IsNullOrEmpty(deviceNet.Address))
            return;

        var address = deviceNet.Address;

        if (!TryGetIdCard(loader, out var idCardUid, out var idCard))
        {
            ReleaseAddress(ent.Comp, address);
            return;
        }

        RebindAgentCard(cartridge, idCardUid.Value);

        if (TryGetContactId(ent, idCardUid.Value, out var contactId)
            || TryLinkAgentCard(ent, cartridge, idCardUid.Value, out contactId))
        {
            BindAddress(ent, contactId, address);
            SyncDepartmentAccess(ent, contactId, idCardUid.Value);
            RememberIdCard(cartridge, idCardUid.Value);
            return;
        }

        ReleaseAddress(ent.Comp, address);

        if (!HasServerAccess(ent, idCardUid.Value))
            return;

        contactId = RegisterNewClient(ent, idCardUid.Value, idCard.FullName ?? "#unkn##n", address);
        SendToContact(ent, contactId, CreateInfoPayload(ent.Comp));
        RememberIdCard(cartridge, idCardUid.Value);
    }

    private void BindAddress(Entity<MessengerServerComponent> ent, ContactUid contactId, string address)
    {
        if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact))
            return;

        foreach (var other in ent.Comp.Contacts.Values)
        {
            if (other.Id != contactId)
                other.ActiveAddresses.Remove(address);
        }

        // a client on a new address does not know the server yet
        if (contact.ActiveAddresses.Add(address))
            _deviceNetwork.QueuePacket(ent, address, CreateInfoPayload(ent.Comp));

        DebugTools.Assert(IsAddressUnique(ent.Comp, address), $"Address {address} belongs to several messenger contacts");
    }

    private static void ReleaseAddress(MessengerServerComponent server, string address)
    {
        foreach (var contact in server.Contacts.Values)
        {
            contact.ActiveAddresses.Remove(address);
        }
    }

    /// <summary>
    /// Creates a contact for the client (id card or AI) and private chats between it and every contact of the server
    /// (including a "favorites" chat with itself), then notifies connected clients.
    /// </summary>
    /// <returns>Id of the new contact.</returns>
    private ContactUid RegisterNewClient(Entity<MessengerServerComponent> ent,
        EntityUid clientUid,
        string fullName,
        string? netAddress)
    {
        var server = ent.Comp;
        var newContact = new MessengerContact
        {
            Id = new ContactUid(server.NextId++),
            Name = fullName,
        };

        if (netAddress != null)
            newContact.ActiveAddresses.Add(netAddress);

        server.Contacts[newContact.Id] = newContact;
        EnsureComp<MessengerClientComponent>(clientUid).Contacts[ent] = newContact.Id;

        var unknownName = Loc.GetString("messenger-unknown-contact");

        // for now every contact can write to every other contact
        foreach (var contactId in server.Contacts.Keys)
        {
            if (!server.Contacts.TryGetValue(contactId, out var contact))
                continue;

            var chatName = contactId == newContact.Id
                ? Loc.GetString("messenger-ui-favorites-chat")
                : Loc.GetString("messenger-server-chat-pair", ("first", contact.Name ?? unknownName), ("second", newContact.Name ?? unknownName));

            var chat = new MessengerChat(chatName,
                MessengerChatKind.Contact,
                new HashSet<ContactUid> { contactId, newContact.Id })
            {
                Id = new ChatUid(server.NextId++),
            };
            server.Chats[chat.Id] = chat;

            AddPrivateChat(server, contactId, chat.Id);
            AddPrivateChat(server, newContact.Id, chat.Id);

            SendToContact(ent, contactId, new NetworkPayload
            {
                [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Chat,
                [MessengerNetworkKey.Chat.ToString()] = chat,
            });

            SendToContact(ent, contactId, new NetworkPayload
            {
                [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Contact,
                [MessengerNetworkKey.Contact.ToString()] = newContact,
            });
        }

        SyncDepartmentAccess(ent, newContact.Id, clientUid);

        DebugTools.Assert(IsConsistent(ent), "Messenger server data is inconsistent after registering a client");

        return newContact.Id;
    }

    /// <summary>
    /// Lets the clone of a body write from the same messenger accounts as the original:
    /// the id card of the clone gets the contacts of the original id card.
    /// Call it when the clone already has its id card.
    /// </summary>
    /// <returns>True if the original is registered on some server and the clone card was linked to it.</returns>
    public bool TryLinkClone(EntityUid original, EntityUid clone)
    {
        if (!_idCard.TryFindIdCard(original, out var originalCard)
            || !_idCard.TryFindIdCard(clone, out var cloneCard))
            return false;

        if (!LinkClient(originalCard, cloneCard))
            return false;

        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            DebugTools.Assert(IsConsistent((serverUid, server)), "Messenger server data is inconsistent after linking a clone");
        }

        if (_containerSystem.TryGetContainingContainer(cloneCard.Owner, out var container)
            && container.ID == PdaComponent.PdaIdSlotId)
            RefreshLoader(container.Owner);

        return true;
    }

    /// <summary>
    /// Makes the client write from the accounts of another client, e.g. a clone from the accounts of the original.
    /// A server where the client has its own account is left alone, unless the accounts are replaced.
    /// </summary>
    /// <param name="replace">Drop the accounts the client had before.</param>
    /// <returns>True if the client got an account.</returns>
    private bool LinkClient(EntityUid from, EntityUid to, bool replace = false)
    {
        if (from == to || !TryComp<MessengerClientComponent>(from, out var source))
            return false;

        var target = EnsureComp<MessengerClientComponent>(to);

        if (replace)
            target.Contacts.Clear();

        var linked = false;

        foreach (var (serverUid, contactId) in source.Contacts)
        {
            if (target.Contacts.TryAdd(serverUid, contactId))
                linked = true;
        }

        if (linked)
            target.IsLinked = true;

        return linked;
    }

    /// <returns>True if the client (id card or AI) has a contact on the server.</returns>
    private bool TryGetContactId(Entity<MessengerServerComponent> ent, EntityUid clientUid, out ContactUid contactId)
    {
        contactId = default;

        return TryComp<MessengerClientComponent>(clientUid, out var client)
               && client.Contacts.TryGetValue(ent, out contactId);
    }

    private bool IsLinkedClient(EntityUid clientUid)
    {
        return TryComp<MessengerClientComponent>(clientUid, out var client) && client.IsLinked;
    }

    /// <summary>
    /// Finds the id card in the pda slot of the loader.
    /// </summary>
    private bool TryGetIdCard(EntityUid? loaderUid,
        [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCard)
    {
        idCardUid = null;
        idCard = null;

        if (loaderUid == null)
            return false;

        if (!_containerSystem.TryGetContainer(loaderUid.Value, PdaComponent.PdaIdSlotId, out var container))
            return false;

        foreach (var containedUid in container.ContainedEntities)
        {
            if (!TryComp(containedUid, out idCard))
                continue;

            idCardUid = containedUid;

            return true;
        }

        return false;
    }
}
