using Content.Server.Actions;
using Content.Shared.Chat;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// This handles telepathy
/// </summary>
public sealed class TelepathySystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathyComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TelepathyComponent, TelepathyActionEvent>(OnTelepathyActionPressed);
    }

    private void OnTelepathyActionPressed(EntityUid uid, TelepathyComponent component, TelepathyActionEvent args)
    {
        var chat = new ChatMessage(
            ChatChannel.Radio,
            "Test Message",
            "",
            NetEntity.Invalid,
            null
        );
        var chatMsg = new MsgChatMessage { Message = chat };
        if (TryComp(uid, out ActorComponent? actor))
            _netMan.ServerSendMessage(chatMsg, actor.PlayerSession.Channel);
    }
    private void OnComponentInit(EntityUid uid, TelepathyComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.TelepathyActionEntity, component.TelepathyAction, uid);
    }
}
