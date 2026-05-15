// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;

namespace Content.Shared.SS220.PolymorphTimer;

public abstract class SharedPolymorphTimerSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphTimerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<PolymorphTimerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<PolymorphTimerComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.PolymorphTimerAlert);
    }

    private void OnShutdown(Entity<PolymorphTimerComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.PolymorphTimerAlert);
    }
}
