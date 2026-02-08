using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.SS220.Mech.Components;

namespace Content.Shared.SS220.Mech.Systems;

public abstract partial class SharedAltMechSystem
{
    private void InitializeRelay()
    {
        //SubscribeLocalEvent<AltMechComponent, GettingAttackedAttemptEvent>(RelayRefToPilot);
    }

    private void RelayToPilot<T>(Entity<AltMechComponent> uid, T args) where T : class
    {
        if (uid.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);
    }

    private void RelayRefToPilot<T>(Entity<AltMechComponent> uid, ref T args) where T :struct
    {
        if (uid.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);

        args = ev.Args;
    }
}
