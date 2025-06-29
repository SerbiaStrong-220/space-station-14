// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Threading;
using Content.Client.SS220.UserInterface.DiscordLink;
using Content.Shared.SS220.Discord;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Timer = Robust.Shared.Timing.Timer;


namespace Content.Client.SS220.Discord;

public sealed class DiscordLinkRequiredState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    private DiscordLinkRequiredGui? _linkGui;
    private readonly CancellationTokenSource _timerCancel = new();

    protected override void Startup()
    {
        _linkGui = new DiscordLinkRequiredGui();
        _userInterfaceManager.StateRoot.AddChild(_linkGui);

        Timer.SpawnRepeating(TimeSpan.FromSeconds(10), () =>
        {
            _netManager.ClientSendMessage(new MsgRecheckDiscordLink());
        },
        _timerCancel.Token);
    }

    protected override void Shutdown()
    {
        _timerCancel.Cancel();
        _linkGui!.Dispose();
    }
}
