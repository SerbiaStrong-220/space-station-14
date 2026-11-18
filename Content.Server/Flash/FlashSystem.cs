using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;

namespace Content.Server.Flash;

//SS220-flash-logging begin
public sealed partial class FlashSystem : SharedFlashSystem
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
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(ent):flash} was activated");
            return;
        }

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(ent):flash}");
    }
}
//SS220-flash-logging end
