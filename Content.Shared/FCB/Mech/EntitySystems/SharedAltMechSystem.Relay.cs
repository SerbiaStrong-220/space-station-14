// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Flash;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.FCB.Mech.Components;

namespace Content.Shared.FCB.Mech.Systems;

public abstract partial class SharedAltMechSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<AltMechPilotComponent, FlashAttemptEvent>(RelayRefToMech);
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

    private void RelayToMech<T>(Entity<AltMechPilotComponent> uid, T args) where T : class
    {
        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(uid.Comp.Mech, ref ev);

        args = ev.Args;
    }

    private void RelayRefToMech<T>(Entity<AltMechPilotComponent> uid, ref T args) where T : struct
    {
        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(uid.Comp.Mech, ref ev);

        args = ev.Args;
    }
}

public interface IMechRelayEvent
{
}
