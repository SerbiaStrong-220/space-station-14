// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem
{
    [Dependency] private AlertsSystem _alerts = default!;

    private void InitializeDollAlert()
    {
        SubscribeLocalEvent<PathologyHolderComponent, MapInitEvent>(OnDollMapInit);
        SubscribeLocalEvent<PathologyHolderComponent, ComponentShutdown>(OnDollShutdown);
    }

    private void OnDollMapInit(Entity<PathologyHolderComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.DollAlert);
    }

    private void OnDollShutdown(Entity<PathologyHolderComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _alerts.ClearAlert(ent.Owner, ent.Comp.DollAlert);
    }
}
