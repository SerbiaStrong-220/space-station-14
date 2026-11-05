// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Shuttles.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.SS220.Shuttles.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class EmergencyShuttleResumeCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public override string Command => "emergencyshuttleresume";
    public override string Description => Loc.GetString("cmd-emergency-shuttle-resume-desc");
    public override string Help => Loc.GetString("cmd-emergency-shuttle-resume-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Help);
            return;
        }

        var shuttle = _systems.GetEntitySystem<EmergencyShuttleSystem>();
        if (!shuttle.ResumeEmergencyDeparture())
        {
            shell.WriteLine(Loc.GetString("emergency-shuttle-command-resume-failed"));
            return;
        }

        if (shuttle.IsEmergencyDeparturePaused)
        {
            shell.WriteLine(Loc.GetString("emergency-shuttle-command-resume-hold-remains"));
            return;
        }

        shell.WriteLine(Loc.GetString("emergency-shuttle-command-resume-success",
            ("seconds", shuttle.EmergencyDepartureSecondsRemaining)));
    }
}
