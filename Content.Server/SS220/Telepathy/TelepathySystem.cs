// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.Chat;
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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathyComponent, TelepathySendEvent>(OnTelepathySend);
        SubscribeLocalEvent<TelepathyComponent, TelepathyAnnouncementSendEvent>(OnTelepathyAnnouncementSend);
    }

    private void OnTelepathyAnnouncementSend(Entity<TelepathyComponent> ent, ref TelepathyAnnouncementSendEvent args)
    {
        SendMessageToEveryoneWithRightChannel(args.TelepathyChannel, args.Message, null);
    }

    private void OnTelepathySend(Entity<TelepathyComponent> ent, ref TelepathySendEvent args)
    {
        SendMessageToEveryoneWithRightChannel(ent.Comp.TelepathyChannelPrototype, args.Message, ent);
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
        var name = nameEv.Name;
        return name;
    }
}
