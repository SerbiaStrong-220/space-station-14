// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Zones.UI;
using Content.Shared.SS220.Zones.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Console;

namespace Content.Client.SS220.Zones.Commands;

[UsedImplicitly]
public sealed partial class ZonesControlWindowCommand : LocalizedCommands
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override string Command => SharedZonesSystem.ZoneCommandsPrefix + "control_window";

    public override string Description => Loc.GetString("cmd-zones-control-window-desc");


    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ui.GetUIController<ZonesControlUIController>().ToggleWindow();
    }
}
