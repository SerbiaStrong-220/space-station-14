// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Access.Components;
using Content.Server.SS220.CartridgeLoader.Cartridges;
using Content.Shared.Examine;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.Messenger;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// The agent id card does not give the agent away: inserted into a pda, it uses the messenger account
/// of the card that was in this pda last, instead of becoming a new contact.
/// </summary>
public sealed partial class MessengerServerSystem
{
    [Dependency] private ExperienceSystem _experience = default!;

    private static readonly ProtoId<KnowledgePrototype> AgentKnowledge = "SyndicateAgentKnowledge";

    private void InitializeAgent()
    {
        SubscribeLocalEvent<AgentIDCardComponent, ExaminedEvent>(OnAgentCardExamined);
        SubscribeLocalEvent<AgentIDCardComponent, GetVerbsEvent<AlternativeVerb>>(OnAgentCardGetVerbs);
    }

    /// <summary>
    /// Only the agents see how the card works with the messenger.
    /// </summary>
    private void OnAgentCardExamined(Entity<AgentIDCardComponent> ent, ref ExaminedEvent args)
    {
        if (!_experience.HaveKnowledge(args.Examiner, AgentKnowledge))
            return;

        if (TryGetBoundContactName(ent, out var name))
            args.PushMarkup(Loc.GetString("messenger-agent-examine-bound", ("name", name)));
        else
            args.PushMarkup(Loc.GetString("messenger-agent-examine-unbound"));

        var locked = TryComp<MessengerClientComponent>(ent, out var client) && client.RebindLocked;
        args.PushMarkup(Loc.GetString(locked
            ? "messenger-agent-examine-rebind-locked"
            : "messenger-agent-examine-rebind-allowed"));
    }

    private void OnAgentCardGetVerbs(Entity<AgentIDCardComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !_experience.HaveKnowledge(args.User, AgentKnowledge))
            return;

        var user = args.User;
        var locked = TryComp<MessengerClientComponent>(ent, out var client) && client.RebindLocked;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(locked ? "messenger-agent-verb-allow-rebind" : "messenger-agent-verb-deny-rebind"),
            Act = () => ToggleRebindLock(ent, user),
        });
    }

    private void ToggleRebindLock(Entity<AgentIDCardComponent> ent, EntityUid user)
    {
        var client = EnsureComp<MessengerClientComponent>(ent);
        client.RebindLocked = !client.RebindLocked;

        _popup.PopupEntity(Loc.GetString(client.RebindLocked
                ? "messenger-agent-popup-rebind-locked"
                : "messenger-agent-popup-rebind-allowed"),
            ent,
            user);
    }

    /// <summary>
    /// Name of the first messenger account the card is bound to.
    /// </summary>
    private bool TryGetBoundContactName(EntityUid cardUid, out string name)
    {
        name = "";

        if (!TryComp<MessengerClientComponent>(cardUid, out var client))
            return false;

        foreach (var (serverUid, contactId) in client.Contacts)
        {
            if (TryComp<MessengerServerComponent>(serverUid, out var server)
                && server.Contacts.TryGetValue(contactId, out var contact))
            {
                name = contact.Name ?? "";
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Binds an agent card that has no account yet to the accounts of the id card that was in the pda before it.
    /// </summary>
    /// <returns>True if the card was bound to a contact of the server.</returns>
    private bool TryLinkAgentCard(Entity<MessengerServerComponent> server,
        EntityUid cartridge,
        EntityUid cardUid,
        out ContactUid contactId)
    {
        contactId = default;

        return HasComp<AgentIDCardComponent>(cardUid)
               && TryGetLastIdCard(cartridge, out var lastCard)
               && LinkClient(lastCard, cardUid)
               && TryGetContactId(server, cardUid, out contactId);
    }

    /// <summary>
    /// An agent card that borrowed accounts takes the accounts of the id card that was in the pda before it,
    /// unless the agent forbade it. Other cards keep their accounts.
    /// </summary>
    private void RebindAgentCard(EntityUid cartridge, EntityUid cardUid)
    {
        if (!HasComp<AgentIDCardComponent>(cardUid)
            || !TryComp<MessengerClientComponent>(cardUid, out var client)
            || !client.IsLinked
            || client.RebindLocked
            || !TryGetLastIdCard(cartridge, out var lastCard))
            return;

        LinkClient(lastCard, cardUid, replace: true);
    }

    private bool TryGetLastIdCard(EntityUid cartridge, out EntityUid lastCard)
    {
        lastCard = default;

        if (!TryComp<MessengerClientCartridgeComponent>(cartridge, out var comp)
            || comp.LastIdCard is not { } card
            || TerminatingOrDeleted(card))
            return false;

        lastCard = card;

        return true;
    }

    /// <summary>
    /// The cartridge remembers the last id card that has an account of its own, the agent cards copy its accounts.
    /// </summary>
    private void RememberIdCard(EntityUid cartridge, EntityUid cardUid)
    {
        if (!IsLinkedClient(cardUid) && TryComp<MessengerClientCartridgeComponent>(cartridge, out var comp))
            comp.LastIdCard = cardUid;
    }
}
