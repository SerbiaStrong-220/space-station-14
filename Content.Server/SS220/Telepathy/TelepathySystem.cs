// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Commands.Generic;
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
    /// key is protoId and value is "free".
    /// </summary>
    private Dictionary<ProtoId<TelepathyChannelPrototype>, bool> _dynamicChannels = new();
    private readonly Color _baseDynamicChannelColor = Color.Lime;
    private const int NumberOfDynamicChannelsCreatedInitialization = 10;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);

        SubscribeLocalEvent<TelepathyComponent, TelepathySendEvent>(OnTelepathySend);
        SubscribeLocalEvent<TelepathyComponent, TelepathyAnnouncementSendEvent>(OnTelepathyAnnouncementSend);
    }

    private void OnRoundStart(RoundStartedEvent args)
    {
        foreach (var channel in _dynamicChannels)
        {
            if (!channel.Value)
                FreeUniqueTelepathyChannel(channel.Key);
        }
    }

    private void OnTelepathyAnnouncementSend(Entity<TelepathyComponent> ent, ref TelepathyAnnouncementSendEvent args)
    {
        SendMessageToEveryoneWithRightChannel(args.TelepathyChannel, args.Message, null);
    }

    private void OnTelepathySend(Entity<TelepathyComponent> ent, ref TelepathySendEvent args)
    {
        SendMessageToEveryoneWithRightChannel(ent.Comp.TelepathyChannelPrototype, args.Message, ent);
    }

    /// <summary>
    /// Tries to get free channel from already constructed and if no exists makes new one
    /// </summary>
    public ProtoId<TelepathyChannelPrototype> TakeUniqueTelepathyChannel(string? nameLocPath = null, Color? color = null)
    {
        foreach (var channel in _dynamicChannels)
        {
            if (channel.Value
                && _prototype.TryIndex(channel.Key, out var prototype)
                && (color == null || prototype.Color == color)
                && (nameLocPath == null || prototype.Name == nameLocPath))
            {
                Log.Debug($"The channel was taken from existing dynamics ones with ID {channel.Key}");
                return channel.Key;
            }
        }

        return InitDynamicChannels(NumberOfDynamicChannelsCreatedInitialization, nameLocPath, color);
    }

    /// <summary>
    /// Returns channel with <paramref name="protoId"/> to free channels pool.
    /// if <paramref name="delete"/> than checks and delete any other TelepathyComponent left with that id.
    /// </summary>
    public void FreeUniqueTelepathyChannel(ProtoId<TelepathyChannelPrototype> protoId, bool delete = true)
    {
        if (_dynamicChannels.TryGetValue(protoId, out var isFree))
        {
            Log.Error($"Tried to free unregistered channel, passed id was {protoId}");
            return;
        }
        if (isFree)
        {
            Log.Warning($"Tried to free already freed channel with id {protoId}");
        }
        else
        {
            Log.Debug($"Fried channel with id {protoId}");
        }
        _dynamicChannels[protoId] = true;

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
    /// Checks if channel freed. Will throw KeyNotFoundException if this channel wasnt found
    /// </summary>
    public bool IsChannelFree(ProtoId<TelepathyChannelPrototype> protoId)
    {
        return _dynamicChannels[protoId];
    }

    public ProtoId<TelepathyChannelPrototype> InitDynamicChannels(int count, string? nameLocPath = null, Color? color = null)
    {
        var lastId = "";
        for (int i = 0; i < count; i++)
        {
            lastId = MakeNewDynamicChannel(nameLocPath, color);
        }
        _prototype.ResolveResults();
        return lastId;
    }

    private ProtoId<TelepathyChannelPrototype> MakeNewDynamicChannel(string? nameLocPath = null, Color? color = null)
    {
        var id = Loc.GetString("unique-telepathy-proto-id", ("id", _dynamicChannels.Count));
        var channelColor = color == null ? _baseDynamicChannelColor : color;
        var channelName = nameLocPath == null ? "unique-telepathy-proto-name" : nameLocPath;
        _prototype.LoadString(Loc.GetString("telepathy-marking", ("id", id), ("name", channelName), ("color", channelColor.Value.ToHexNoAlpha()))
                                            .ReplaceLineEndings(Environment.NewLine + "  "));

        _dynamicChannels.Add(id, false);
        Log.Debug($"The channel with ID {id} was added to dynamics ones and used.");
        return id;
    }

    /// <summary>
    /// Used if we need only one instance of prototype. Actually better to use InitDynamicChannels with some count
    /// </summary>
    private ProtoId<TelepathyChannelPrototype> MakeNewDynamicChannelAndResolveResult(string? nameLocPath = null, Color? color = null)
    {
        var id = MakeNewDynamicChannel(nameLocPath, color);
        _prototype.ResolveResults();
        return id;
    }


    private void SendMessageToEveryoneWithRightChannel(ProtoId<TelepathyChannelPrototype> rightTelepathyChanel, string message, EntityUid? senderUid)
    {
        var telepathyQuery = EntityQueryEnumerator<TelepathyComponent>();
        while (telepathyQuery.MoveNext(out var receiverUid, out var receiverTelepathy))
        {
            if (rightTelepathyChanel == receiverTelepathy.TelepathyChannelPrototype)
                SendMessageToChat(receiverUid, message, senderUid, _prototype.Index(rightTelepathyChanel));
        }
    }


    private void SendMessageToChat(EntityUid receiverUid, string messageString, EntityUid? senderUid, TelepathyChannelPrototype telepathyChannel)
    {
        var netSource = _entityManager.GetNetEntity(receiverUid);
        var wrappedMessage = GetWrappedTelepathyMessage(messageString, senderUid, telepathyChannel);
        var message = new ChatMessage(
            ChatChannel.Telepathy,
            messageString,
            wrappedMessage,
            netSource,
            null
        );
        if (TryComp(receiverUid, out ActorComponent? actor))
            _netMan.ServerSendMessage(new MsgChatMessage() {Message = message}, actor.PlayerSession.Channel);
    }

    private string GetWrappedTelepathyMessage(string messageString, EntityUid? senderUid, TelepathyChannelPrototype telepathyChannel)
    {
        if (senderUid == null)
        {
            return Loc.GetString(
                "chat-manager-send-telepathy-announce",
                ("announce", FormattedMessage.EscapeText(messageString))
            );
        }

        return Loc.GetString(
            "chat-manager-send-telepathy-message",
            ("channel", $"\\[{telepathyChannel.LocalizedName}\\]"),
            ("message", FormattedMessage.EscapeText(messageString)),
            ("senderName", GetSenderName(senderUid)),
            ("color", telepathyChannel.Color)
        );
    }

    private string GetSenderName(EntityUid? senderUid)
    {
        var nameEv = new TransformSpeakerNameEvent(senderUid!.Value, Name(senderUid.Value));
        RaiseLocalEvent(senderUid.Value, nameEv);
        var name = Name(nameEv.Sender);
        return name;
    }
}
