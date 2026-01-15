using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;

namespace Content.Shared.SS220.Mech.Systems;

public abstract partial class SharedAltMechSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<MechComponent, GettingAttackedAttemptEvent>(RelayRefToPilot);
    }

    private void RelayToPilot<T>(Entity<MechComponent> uid, T args) where T : class
    {
        if (uid.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);
    }

    private void RelayRefToPilot<T>(Entity<MechComponent> uid, ref T args) where T :struct
    {
        if (uid.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);

        args = ev.Args;
    }
}
