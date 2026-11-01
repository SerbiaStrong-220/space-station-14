// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.SS220.Telepathy;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.UpdateChannels;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This handles events related to sending messages over the telepathy channel
/// </summary>
public sealed partial class TelepathySystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    /// <summary>
    /// Key is a "fake" protoId. It wont indexed. Value is the prototype the channel was made from.
    /// </summary>
    private readonly SortedDictionary<ProtoId<TelepathyChannelPrototype>, ProtoId<TelepathyChannelPrototype>> _dynamicChannels = new();
    private readonly HashSet<ProtoId<TelepathyChannelPrototype>> _privateChannels = new();
    private int _nextDynamicChannelId;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);

        SubscribeLocalEvent<TelepathyComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TelepathyComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<TelepathyComponent, TelepathySendEvent>(OnTelepathySend);
        SubscribeLocalEvent<TelepathyAnnouncementSendEvent>(OnTelepathyAnnouncementSend);
    }

    private void OnRoundStart(RoundStartedEvent args)
    {
        foreach (var channel in _dynamicChannels.Keys.ToList())
        {
            FreeUniqueTelepathyChannel(channel);
        }
    }

    private void OnComponentInit(Entity<TelepathyComponent> ent, ref ComponentInit args)
    {
        if (TryComp<ActorComponent>(ent.Owner, out var actor))
            RaiseNetworkEvent(new UpdateChannelEvent(), actor.PlayerSession);
    }

    private void OnComponentRemove(Entity<TelepathyComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<ActorComponent>(ent.Owner, out var actor))
            RaiseNetworkEvent(new UpdateChannelEvent(), actor.PlayerSession);

        if (ent.Comp.TelepathyChannelPrototype is { } channel)
            CloseIfAbandoned(channel, ent.Owner);
    }

    private void OnTelepathyAnnouncementSend(TelepathyAnnouncementSendEvent args)
    {
        SendMessageToEveryoneWithRightChannel(args.TelepathyChannel, args.Message, null);
    }

    private void OnTelepathySend(Entity<TelepathyComponent> ent, ref TelepathySendEvent args)
    {
        if (ent.Comp.TelepathyChannelPrototype is not { } telepathyChannel)
            return;

        if (!CanSendTelepathy(ent))
            return;

        SendMessageToEveryoneWithRightChannel(telepathyChannel, args.Message, ent);
    }

    /// <summary>
    /// Makes new channel with the parameters of <paramref name="template"/>, ghost hearing settings of the template apply to it.
    /// </summary>
    /// <param name="isPrivate">If true, <see cref="GetJoinableChannel"/> doesn't offer this channel to other systems.</param>
    public ProtoId<TelepathyChannelPrototype> TakeUniqueTelepathyChannel(ProtoId<TelepathyChannelPrototype> template, bool isPrivate = false)
    {
        var channel = MakeNewDynamicChannel(template);

        if (isPrivate)
            _privateChannels.Add(channel);

        return channel;
    }

    /// <summary>
    /// Returns the prototype <paramref name="channel"/> was made from, or the channel itself if there is none.
    /// </summary>
    public ProtoId<TelepathyChannelPrototype> GetChannelTemplate(ProtoId<TelepathyChannelPrototype> channel)
    {
        return _dynamicChannels.GetValueOrDefault(channel, channel);
    }

    /// <summary>
    /// Returns channel with <paramref name="protoId"/> to free channels pool.
    /// if <paramref name="delete"/> than checks and delete any other TelepathyComponent left with that id.
    /// </summary>
    public void FreeUniqueTelepathyChannel(ProtoId<TelepathyChannelPrototype> protoId, bool delete = true)
    {
        if (!_dynamicChannels.TryGetValue(protoId, out _)) // SS220 removing-telepathy-from-a-slave fix
        {
            Log.Error($"Tried to free unregistered channel, passed id was {protoId}");
            return;
        }

        Log.Debug($"Freed channel with id {protoId}");
        _dynamicChannels.Remove(protoId);
        _privateChannels.Remove(protoId);
        var query = EntityQueryEnumerator<TelepathyComponent>();
        while (query.MoveNext(out var uid, out var telepathyComponent))
        {
            if (telepathyComponent.TelepathyChannelPrototype == protoId
                && !telepathyComponent.Deleted)
            {
                if (delete)
                    RemComp(uid, telepathyComponent);
                else
                    Log.Warning($"Fried channel, but telepathy components with this id {protoId} still exists");
            }
        }
    }

    /// <summary>
    /// Returns the channel of <paramref name="uid"/> that others may join, null if it has none or the channel is private.
    /// </summary>
    public ProtoId<TelepathyChannelPrototype>? GetJoinableChannel(EntityUid uid)
    {
        var channel = CompOrNull<TelepathyComponent>(uid)?.TelepathyChannelPrototype;
        return channel is { } id && _privateChannels.Contains(id) ? null : channel;
    }

    /// <summary>
    /// Moves <paramref name="uid"/> to <paramref name="channel"/>, giving it telepathy if needed.
    /// </summary>
    public void SetChannel(EntityUid uid, ProtoId<TelepathyChannelPrototype> channel, bool canSend = true)
    {
        var telepathy = EnsureComp<TelepathyComponent>(uid);
        var oldChannel = telepathy.TelepathyChannelPrototype;

        if (oldChannel == channel && telepathy.CanSend == canSend)
            return;

        telepathy.CanSend = canSend;
        telepathy.TelepathyChannelPrototype = channel;

        var ev = new TelepathyChannelChangedEvent();
        RaiseLocalEvent(uid, ref ev);

        // After the event, so that entities following this one have already left the old channel too.
        if (oldChannel is { } old && old != channel)
            CloseIfAbandoned(old);
    }

    /// <summary>
    /// Takes telepathy from <paramref name="uid"/>, unless <see cref="GetFallbackTelepathyChannelEvent"/> names a channel to move it to.
    /// </summary>
    public void RemoveTelepathy(EntityUid uid)
    {
        if (!TryComp<TelepathyComponent>(uid, out var telepathy))
            return;

        var fallback = new GetFallbackTelepathyChannelEvent();
        RaiseLocalEvent(uid, ref fallback);

        if (fallback.Channel is { } channel)
        {
            SetChannel(uid, channel);
            return;
        }

        RemComp(uid, telepathy);

        var ev = new TelepathyChannelChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// A dynamic channel exists to connect its members, so with one member left it closes and takes that member's telepathy.
    /// </summary>
    private void CloseIfAbandoned(ProtoId<TelepathyChannelPrototype> channel, EntityUid? leaving = null)
    {
        if (!_dynamicChannels.ContainsKey(channel))
            return;

        EntityUid? lastMember = null;
        var query = EntityQueryEnumerator<TelepathyComponent>();
        while (query.MoveNext(out var uid, out var telepathy))
        {
            if (uid == leaving || telepathy.TelepathyChannelPrototype != channel)
                continue;

            if (lastMember != null)
                return;

            lastMember = uid;
        }

        _dynamicChannels.Remove(channel);
        _privateChannels.Remove(channel);

        if (lastMember != null)
            RemoveTelepathy(lastMember.Value);
    }

    private ProtoId<TelepathyChannelPrototype> MakeNewDynamicChannel(ProtoId<TelepathyChannelPrototype> template)
    {
        var id = Loc.GetString("unique-telepathy-proto-id", ("id", _nextDynamicChannelId++));

        _dynamicChannels.Add(id, template);

        Log.Debug($"The channel with ID {id} was added to dynamics ones and used.");
        return id;
    }

    private void SendMessageToEveryoneWithRightChannel(ProtoId<TelepathyChannelPrototype> rightTelepathyChannel, string message, EntityUid? senderUid)
    {
        if (!_prototype.TryIndex(GetChannelTemplate(rightTelepathyChannel), out var prototype))
        {
            Log.Error($"Tried to send message with incorrect {nameof(TelepathyChannelPrototype)} proto id. id was: {rightTelepathyChannel}");
            return;
        }

        List<EntityUid> telephatyTtsRecievers = [];

        var telepathyQuery = EntityQueryEnumerator<TelepathyComponent>();
        while (telepathyQuery.MoveNext(out var receiverUid, out var receiverTelepathy))
        {
            if (rightTelepathyChannel == receiverTelepathy.TelepathyChannelPrototype || receiverTelepathy.ReceiveAllChannels)
            {
                SendMessageToChat(receiverUid, message, senderUid, prototype.ChannelParameters);
                telephatyTtsRecievers.Add(receiverUid);
            }
        }

        if (senderUid != null)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telepathy message were send from {ToPrettyString(senderUid):user}: {message}, telepathy channel: {rightTelepathyChannel.Id}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Announce telepathy message: {message}, were send in telepathy channel: {rightTelepathyChannel.Id}");

        if (senderUid != null && HasComp<TTSComponent>(senderUid))
        {
            RaiseLocalEvent(new TelepathySpokeEvent(senderUid.Value, message, [.. telephatyTtsRecievers], rightTelepathyChannel));
        }
    }


    private void SendMessageToChat(EntityUid receiverUid, string messageString, EntityUid? senderUid, ChannelParameters telepathyChannelParameters)
    {
        var netSource = _entityManager.GetNetEntity(receiverUid);
        var wrappedMessage = GetWrappedTelepathyMessage(messageString, senderUid, telepathyChannelParameters);
        var message = new ChatMessage(
            ChatChannel.Telepathy,
            messageString,
            wrappedMessage,
            netSource,
            null
        );

        if (!TryComp(receiverUid, out ActorComponent? actor))
            return;

        if (actor.PlayerSession == null)
            return;

        _netMan.ServerSendMessage(new MsgChatMessage() { Message = message }, actor.PlayerSession.Channel);
    }

    private string GetWrappedTelepathyMessage(string messageString, EntityUid? senderUid, ChannelParameters telepathyChannelParameters)
    {
        if (senderUid == null)
        {
            return Loc.GetString(
                "chat-manager-send-telepathy-announce",
                ("announce", FormattedMessage.EscapeText(messageString)),
                 ("channel", $"\\[{Loc.GetString(telepathyChannelParameters.Name)}\\]"),
                ("color", telepathyChannelParameters.Color)
            );
        }

        return Loc.GetString(
            "chat-manager-send-telepathy-message",
            ("channel", $"\\[{Loc.GetString(telepathyChannelParameters.Name)}\\]"),
            ("message", FormattedMessage.EscapeText(messageString)),
            ("senderName", GetSenderName(senderUid)),
            ("color", telepathyChannelParameters.Color)
        );
    }

    private string GetSenderName(EntityUid? senderUid)
    {
        var nameEv = new TransformSpeakerNameEvent(senderUid!.Value, Name(senderUid.Value));
        RaiseLocalEvent(senderUid.Value, nameEv);
        var name = Name(nameEv.Sender);
        return name;
    }

    private bool CanSendTelepathy(EntityUid sender)
    {
        var args = new TelepathySendAttemptEvent(sender, false);
        RaiseLocalEvent(sender, ref args);
        return !args.Cancelled;
    }
}
