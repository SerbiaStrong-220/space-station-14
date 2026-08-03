// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.SS220.TTS;
using Robust.Shared.Console;

namespace Content.Server.SS220.TTS.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class TtsClearClientsQueuesCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private IChatManager _chat = default!;

    public override string Command => SharedTtsSystem.TtsCommandsPrefix + "clear_clients_queues";
    public override string Description => Loc.GetString("cmd-tts-clear-clients-queues-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ttsSys = _entitySystemManager.GetEntitySystem<TtsSystem>();
        ttsSys.RequestResetAllClientQueues();

        _chat.DispatchServerAnnouncement(Loc.GetString("command-tts-clear-request-dispatch"));
        shell.WriteLine(Loc.GetString("cmd-tts-clear-clients-queues-request-sended"));
    }
}
