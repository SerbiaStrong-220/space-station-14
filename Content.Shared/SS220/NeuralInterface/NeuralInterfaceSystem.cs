// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.Mindshield.Components;
using Content.Shared.SS220.MindShield;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class NeuralInterfaceSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NeuralInterfaceComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<NeuralInterfaceComponent, MindshieldProtectionGrantedEvent>(OnProtectionGranted);
        SubscribeLocalEvent<NeuralInterfaceComponent, MindshieldProtectionRemovedEvent>(OnProtectionRemoved);

        base.Initialize();
    }

    private void OnCompStartup(Entity<NeuralInterfaceComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.InterfaceType == 0 && HasComp<MindShieldComponent>(ent.Owner))
            ++ent.Comp.InterfaceType;

        _alerts.ShowAlert(ent.Owner, ent.Comp.InterfaceAlertProto, (short)ent.Comp.InterfaceType);
    }

    private void OnProtectionGranted(Entity<NeuralInterfaceComponent> ent, ref MindshieldProtectionGrantedEvent args)
    {
        if (ent.Comp.InterfaceType == 0 && HasComp<MindShieldComponent>(ent.Owner))
            ++ent.Comp.InterfaceType;

        _alerts.ShowAlert(ent.Owner, ent.Comp.InterfaceAlertProto, (short)ent.Comp.InterfaceType);
    }

    private void OnProtectionRemoved(Entity<NeuralInterfaceComponent> ent, ref MindshieldProtectionRemovedEvent args)
    {
        if (ent.Comp.InterfaceType == 1)
            --ent.Comp.InterfaceType;

        _alerts.ShowAlert(ent.Owner, ent.Comp.InterfaceAlertProto, (short)ent.Comp.InterfaceType);
    }
}
