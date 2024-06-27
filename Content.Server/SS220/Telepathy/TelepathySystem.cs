// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
        SendMessageToEveryoneWithRightChannel(args.TelepathyChannel, args.Message);
    }

    private void OnTelepathySend(EntityUid uid, TelepathyComponent component, TelepathySendEvent args)
    {
        if (!HasComp<TelepathyComponent>(uid))
            return;

        SendMessageToEveryoneWithRightChannel(component.TelepathyChannelPrototype, args.Message);
    }

    private void SendMessageToEveryoneWithRightChannel(string rightTelepathyChanel, string message)
    {
        var telepathyQuery = EntityQueryEnumerator<TelepathyComponent>();
        while (telepathyQuery.MoveNext(out var receiverUid, out var receiverTelepathy))
        {
            if (rightTelepathyChanel == receiverTelepathy.TelepathyChannelPrototype)
                SendMessageToChat(receiverUid, message);
        }
    }


    private void SendMessageToChat(EntityUid uid, string messageString)
    {
        var netSource = _entityManager.GetNetEntity(uid);
        var wrappedMessage = Loc.GetString(
            "chat-manager-server-wrap-message",
            ("message", FormattedMessage.EscapeText(messageString))
        );
        var message = new ChatMessage(
            ChatChannel.Telepathy,
            messageString,
            wrappedMessage,
            netSource,
            null
        );
        if (TryComp(uid, out ActorComponent? actor))
            _netMan.ServerSendMessage(new MsgChatMessage() {Message = message}, actor.PlayerSession.Channel);
    }
}
