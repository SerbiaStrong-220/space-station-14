// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;

namespace Content.Server.SS220.Flash;

public sealed partial class FlashLoggingSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashComponent, AfterFlashActivatedEvent>(OnFlashActivated);
    }

    private void OnFlashActivated(Entity<FlashComponent> ent, ref AfterFlashActivatedEvent args)
    {
        if (args.User is not { } user)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"Flash {ToPrettyString(ent):flash} was activated");
            return;
        }

        if (args.Target is { } target)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} activated flash {ToPrettyString(ent):flash} on {ToPrettyString(target):target}");
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} activated flash {ToPrettyString(ent):flash}");
    }
}
