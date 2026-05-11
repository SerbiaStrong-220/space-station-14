using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.SS220.Players;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Shared.Network;

namespace Content.Client.SS220.ChatBans;

public sealed partial class ChatRequirementsManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly INetManager _net = default!;

    private ISawmill _sawmill = default!;

    public event Action? Updated;

    private readonly List<BannableChats> _chatBans = [];

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat_requirements");

        _net.RegisterNetMessage<MsgChatsBans>(ReceiveChatBans);

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel is ClientRunLevel.Initialize)
            _chatBans.Clear();
    }

    private void ReceiveChatBans(MsgChatsBans msg)
    {
        _sawmill.Debug($"Received chat ban info containing {msg.Bans.Count} entries.");

        _chatBans.Clear();
        _chatBans.AddRange(msg.Bans);
        Updated?.Invoke();
    }

    [PublicAPI]
    public bool IsBanned(ChatSelectChannel channel)
    {
        var chatName = Enum.GetName(channel);
        if (Enum.TryParse<BannableChats>(chatName, out var banType) && _chatBans.Contains(banType))
            return true;

        return false;
    }
}
