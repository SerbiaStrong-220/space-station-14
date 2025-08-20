// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Zones.Systems;
using Content.Shared.SS220.Zones.Systems;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.SS220.Zones.Commands;

[UsedImplicitly]
public sealed partial class ZoneControlWindowCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => SharedZonesSystem.ZoneCommandsPrefix + "control_window";

    public override string Description => Loc.GetString("zone-command-control-window-desc");


    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var window = _entityManager.System<ZonesSystem>().ControlWindow;
        if (window.IsOpen)
            return;

        window.OpenCentered();
    }
}
