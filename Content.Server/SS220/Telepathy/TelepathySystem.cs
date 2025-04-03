// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This handles events related to sending messages over the telepathy channel
/// </summary>
public sealed class TelepathySystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Key is a "fake" protoId. It wont indexed.
    /// </summary>
    private SortedDictionary<ProtoId<TelepathyChannelPrototype>, ChannelParameters> _dynamicChannels = new();
    private readonly Color _baseDynamicChannelColor = Color.Lime;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);

        SubscribeLocalEvent<TelepathyComponent, TelepathySendEvent>(OnTelepathySend);
        SubscribeLocalEvent<TelepathyAnnouncementSendEvent>(OnTelepathyAnnouncementSend);
    }

    private void OnRoundStart(RoundStartedEvent args)
    {
        foreach (var channel in _dynamicChannels)
        {
            FreeUniqueTelepathyChannel(channel.Key);
        }
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
    /// Tries to get free channel from already constructed and if no exists makes new one
    /// </summary>
    public ProtoId<TelepathyChannelPrototype> TakeUniqueTelepathyChannel(string? nameLocPath = null, Color? color = null)
    {
        return MakeNewDynamicChannel(nameLocPath, color);
    }

    /// <summary>
    /// Returns channel with <paramref name="protoId"/> to free channels pool.
    /// if <paramref name="delete"/> than checks and delete any other TelepathyComponent left with that id.
    /// </summary>
    public void FreeUniqueTelepathyChannel(ProtoId<TelepathyChannelPrototype> protoId, bool delete = true)
    {
        if (_dynamicChannels.TryGetValue(protoId, out var _))
        {
            Log.Error($"Tried to free unregistered channel, passed id was {protoId}");
            return;
        }

        Log.Debug($"Freed channel with id {protoId}");
        _dynamicChannels.Remove(protoId);
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

    private ProtoId<TelepathyChannelPrototype> MakeNewDynamicChannel(string? nameLocPath = null, Color? color = null)
    {
        var id = Loc.GetString("unique-telepathy-proto-id", ("id", _dynamicChannels.Count));

        var channelColor = color ?? _baseDynamicChannelColor;
        var channelName = nameLocPath ?? "unique-telepathy-proto-name";

        _dynamicChannels.Add(id, new ChannelParameters(channelName, channelColor));

        Log.Debug($"The channel with ID {id} was added to dynamics ones and used.");
        return id;
    }

    private void SendMessageToEveryoneWithRightChannel(ProtoId<TelepathyChannelPrototype> rightTelepathyChannel, string message, EntityUid? senderUid)
    {
        ChannelParameters? channelParameters = null;
        if (_dynamicChannels.TryGetValue(rightTelepathyChannel, out var dynamicParameters))
            channelParameters = dynamicParameters;
        if (_prototype.TryIndex(rightTelepathyChannel, out var prototype))
            channelParameters = prototype.ChannelParameters;
        if (channelParameters == null)
        {
            Log.Error($"Tried to send message with incorrect {nameof(TelepathyChannelPrototype)} proto id. id was: {rightTelepathyChannel}");
            return;
        }

        var telepathyQuery = EntityQueryEnumerator<TelepathyComponent>();
        while (telepathyQuery.MoveNext(out var receiverUid, out var receiverTelepathy))
        {
            if (rightTelepathyChannel == receiverTelepathy.TelepathyChannelPrototype || receiverTelepathy.ReceiveAllChannels)
                SendMessageToChat(receiverUid, message, senderUid, channelParameters);
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
        if (TryComp(receiverUid, out ActorComponent? actor))
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
