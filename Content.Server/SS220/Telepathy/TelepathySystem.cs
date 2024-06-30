// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This handles events related to sending messages over the telepathy channel
/// </summary>
public sealed class TelepathySystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathyComponent, TelepathySendEvent>(OnTelepathySend);
        SubscribeLocalEvent<TelepathyComponent, TelepathyAnnouncementSendEvent>(OnTelepathyAnnouncementSend);
    }

    private void OnTelepathyAnnouncementSend(EntityUid uid, TelepathyComponent component, TelepathyAnnouncementSendEvent args)
    {
        SendMessageToEveryoneWithRightChannel(args.TelepathyChannel, args.Message, null);
    }

    private void OnTelepathySend(EntityUid senderUid, TelepathyComponent component, TelepathySendEvent args)
    {
        if (!HasComp<TelepathyComponent>(senderUid))
            return;

        SendMessageToEveryoneWithRightChannel(component.TelepathyChannelPrototype, args.Message, senderUid);
    }

    private void SendMessageToEveryoneWithRightChannel(string rightTelepathyChanel, string message, EntityUid? senderUid)
    {
        var telepathyQuery = EntityQueryEnumerator<TelepathyComponent>();
        while (telepathyQuery.MoveNext(out var receiverUid, out var receiverTelepathy))
        {
            if (rightTelepathyChanel == receiverTelepathy.TelepathyChannelPrototype)
                SendMessageToChat(receiverUid, message, senderUid);
        }
    }


    private void SendMessageToChat(EntityUid receiverUid, string messageString, EntityUid? senderUid)
    {
        var name = GetSenderName(senderUid);

        var netSource = _entityManager.GetNetEntity(receiverUid);
        var wrappedMessage = GetWrappedTelepathyMessage(receiverUid, messageString, senderUid);
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

    private string GetWrappedTelepathyMessage(EntityUid receiverUid, string messageString, EntityUid? senderUid)
    {
        if (senderUid == null)
        {
            return Loc.GetString(
                "chat-manager-send-telepathy-announce",
                ("announce", FormattedMessage.EscapeText(messageString))
            );
        }

        return  Loc.GetString(
            "chat-manager-send-telepathy-message",
            ("message", FormattedMessage.EscapeText(messageString)),
            ("senderName", GetSenderName(senderUid))
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
