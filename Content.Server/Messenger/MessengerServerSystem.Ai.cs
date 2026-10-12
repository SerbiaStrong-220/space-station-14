// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.AlertLevel;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Messenger;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Messenger;

/// <summary>
/// Read-only access of the station AI to chats of the server. It is granted only on high station alert levels.
/// </summary>
public sealed partial class MessengerServerSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedStationSystem _station = default!;

    private const int MaxAiChats = 220;
    private const int MaxAiHistoryMessages = 200;

    private static readonly SpriteSpecifier.Texture AiReadIcon =
        new(new ResPath("/Textures/Interface/VerbIcons/information.svg.192dpi.png"));

    private void InitializeAi()
    {
        SubscribeLocalEvent<MessengerServerComponent, GetVerbsEvent<Verb>>(OnAiGetVerbs);
        SubscribeLocalEvent<MessengerServerComponent, ExaminedEvent>(OnAiExamined);
        SubscribeLocalEvent<MessengerServerComponent, MessengerAiRequestMessage>(OnAiRequest);

        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    private void OnAiGetVerbs(Entity<MessengerServerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!HasComp<StationAiHeldComponent>(args.User) || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        var allowed = IsAiReadAllowed(ent, out var deniedReason);

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("messenger-ai-verb-read"),
            Message = deniedReason,
            Disabled = !allowed,
            Icon = AiReadIcon,
            Act = () => OpenAiUi(ent, user),
        });
    }

    private void OnAiExamined(Entity<MessengerServerComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<StationAiHeldComponent>(args.Examiner))
            return;

        var levels = new List<string>(ent.Comp.AiReadAlertLevels.Count);

        foreach (var level in ent.Comp.AiReadAlertLevels)
        {
            levels.Add(Loc.TryGetString($"alert-level-{level}", out var name) ? name : level);
        }

        args.PushMarkup(Loc.GetString("messenger-ai-examine", ("levels", string.Join(", ", levels))));
    }

    private void OnAiRequest(Entity<MessengerServerComponent> ent, ref MessengerAiRequestMessage args)
    {
        if (!HasComp<StationAiHeldComponent>(args.Actor) || !IsAiReadAllowed(ent, out _))
            return;

        SendAiView(ent, args.Actor, args.ChatId);
    }

    /// <summary>
    /// Closes the reading windows when the station leaves the alert levels that allow reading.
    /// </summary>
    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var uid, out var server))
        {
            if (_station.GetOwningStation(uid) != args.Station || server.AiReadAlertLevels.Contains(args.AlertLevel))
                continue;

            _ui.CloseUi(uid, MessengerAiUiKey.Key);
        }
    }

    /// <param name="deniedReason">Localized reason why the reading is not available, null if it is.</param>
    /// <returns>True if the station AI may read chats of the server right now.</returns>
    private bool IsAiReadAllowed(Entity<MessengerServerComponent> ent, [NotNullWhen(false)] out string? deniedReason)
    {
        deniedReason = null;

        if (!IsPowered(ent))
        {
            deniedReason = Loc.GetString("messenger-ai-denied-unpowered");
            return false;
        }

        if (_station.GetOwningStation(ent) is not { } station || !TryComp<AlertLevelComponent>(station, out var alert))
        {
            deniedReason = Loc.GetString("messenger-ai-denied-no-station");
            return false;
        }

        if (ent.Comp.AiReadAlertLevels.Contains(alert.CurrentLevel))
            return true;

        var levelName = Loc.TryGetString($"alert-level-{alert.CurrentLevel}", out var name) ? name : alert.CurrentLevel;
        deniedReason = Loc.GetString("messenger-ai-denied-alert-level", ("level", levelName));

        return false;
    }

    private void OpenAiUi(Entity<MessengerServerComponent> ent, EntityUid user)
    {
        if (!IsAiReadAllowed(ent, out _))
            return;

        _adminLogger.Add(LogType.MessengerServer,
            LogImpact.Medium,
            $"AI {ToPrettyString(user):user} opened chat logs of messenger server {ToPrettyString(ent):entity}");

        _ui.OpenUi(ent.Owner, MessengerAiUiKey.Key, user);
    }

    /// <summary>
    /// Sends chats that have messages (most recent first) and the history of the requested chat.
    /// </summary>
    private void SendAiView(Entity<MessengerServerComponent> ent, EntityUid actor, uint? openedChatId)
    {
        DebugTools.Assert(IsAiReadAllowed(ent, out _), "Chats are sent to the AI while reading is not allowed");

        var server = ent.Comp;
        var chats = new List<MessengerChat>();

        foreach (var chat in server.Chats.Values)
        {
            if (chat.LastMessageId != null)
                chats.Add(chat);
        }

        chats.Sort((a, b) => b.LastMessageId!.Value.CompareTo(a.LastMessageId!.Value));

        if (chats.Count > MaxAiChats)
            chats.RemoveRange(MaxAiChats, chats.Count - MaxAiChats);

        // the opened chat must be in the view even if it is not among the most recent ones
        MessengerChat? openedChat = null;

        if (openedChatId != null && server.Chats.TryGetValue(openedChatId.Value, out openedChat))
        {
            if (!chats.Contains(openedChat) && openedChat.LastMessageId != null)
                chats.Add(openedChat);
        }

        var view = new MessengerAiViewMessage
        {
            ServerName = server.Name,
            OpenedChatId = openedChatId,
        };

        foreach (var chat in chats)
        {
            view.Chats.Add(new MessengerChat(chat.Name, chat.Kind, new HashSet<uint>(chat.MembersId))
            {
                Id = chat.Id,
                LastMessageId = chat.LastMessageId,
            });

            if (server.Messages.TryGetValue(chat.LastMessageId!.Value, out var lastMessage))
                view.Messages[lastMessage.Id] = lastMessage;

            foreach (var memberId in chat.MembersId)
            {
                if (server.Contacts.TryGetValue(memberId, out var contact))
                    view.ContactNames[memberId] = contact.Name ?? "";
            }
        }

        if (openedChat != null)
            AddAiHistory(server, openedChat, view);

        _ui.ServerSendUiMessage(ent.Owner, MessengerAiUiKey.Key, view, actor);
    }

    private static void AddAiHistory(MessengerServerComponent server, MessengerChat chat, MessengerAiViewMessage view)
    {
        var messageIds = new List<uint>(chat.MessagesId);
        messageIds.Sort();

        for (var i = Math.Max(0, messageIds.Count - MaxAiHistoryMessages); i < messageIds.Count; i++)
        {
            if (server.Messages.TryGetValue(messageIds[i], out var message))
                view.Messages[message.Id] = message;
        }
    }
}
