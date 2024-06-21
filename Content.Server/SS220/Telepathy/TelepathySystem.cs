using Content.Server.Actions;
using Content.Shared.Chat;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This handles telepathy
/// </summary>
public sealed class TelepathySystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathyComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TelepathyComponent, TelepathyActionEvent>(OnTelepathyActionPressed);
    }

    private void OnTelepathyActionPressed(EntityUid uid, TelepathyComponent component, TelepathyActionEvent args)
    {
        var netSource = _entityManager.GetNetEntity(uid);
        var testMessageText = "Test message";
        var wrappedMessage = Loc.GetString(
            "chat-manager-server-wrap-message",
            ("message", FormattedMessage.EscapeText(testMessageText))
        );
        var message = new ChatMessage(
            ChatChannel.Telepathy,
            testMessageText,
            wrappedMessage,
            netSource,
            null
        );
        if (TryComp(uid, out ActorComponent? actor))
            _netMan.ServerSendMessage(new MsgChatMessage() {Message = message}, actor.PlayerSession.Channel);
    }
    private void OnComponentInit(EntityUid uid, TelepathyComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.TelepathyActionEntity, component.TelepathyAction, uid);
    }
}
