// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.Console;

namespace Content.Client.SS220.TTS.Commands;

public sealed partial class TtsClearLocalQueuesCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;

    public override string Command => SharedTtsSystem.TtsCommandsPrefix + "clear_local_queues";
    public override string Description => Loc.GetString("cmd-tts-clear-local-queues-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ttsSys = _entitySystemManager.GetEntitySystem<TtsSystem>();
        ttsSys.ClearAllQueuesAndStreams();

        shell.WriteLine(Loc.GetString("cmd-tts-clear-local-queues-success"));
    }
}
