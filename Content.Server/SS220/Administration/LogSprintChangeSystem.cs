// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.SS220.Movement.Events;

namespace Content.Server.SS220.Administration;

public sealed class LogSprintChangeSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprintChangedEvent>(OnSprintChanged);
    }

    private void OnSprintChanged(ref SprintChangedEvent args)
    {
        var action = args.Sprinting ? "sprint" : "walk";
        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Uid)} changed movement mode to {action}");
    }
}
