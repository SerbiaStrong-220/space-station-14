// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.Console;

namespace Content.Server.SS220.TTS.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class TtsClearAllClientsQueuesCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private IChatManager _chat = default!;

    public override string Command => SharedTtsSystem.TtsCommandsPrefix + "clear_all_clients_queues";
    public override string Description => Loc.GetString("cmd-tts-clear-clients-queues-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ttsSys = _entitySystemManager.GetEntitySystem<TtsSystem>();
        ttsSys.ClearClientQueues();

        shell.WriteLine(Loc.GetString("cmd-tts-clear-clients-queues-request-sended"));
        _chat.DispatchServerAnnouncement(Loc.GetString("cmd-tts-clear-clients-queues-public-announcement"));
    }
}
