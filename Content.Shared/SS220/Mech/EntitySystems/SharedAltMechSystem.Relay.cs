// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Atmos;
using Content.Shared.Flash;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.SS220.Mech.Components;

namespace Content.Shared.SS220.Mech.Systems;

public abstract partial class SharedAltMechSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<AltMechPilotComponent, FlashAttemptEvent>(RelayRefToMech);
        SubscribeLocalEvent<AltMechPilotComponent, GetFireProtectionEvent>(RelayToMech);
    }

    protected void RelayToPilot<T>(Entity<AltMechComponent> ent, T args) where T : class
    {
        if (ent.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);
    }

    protected void RelayRefToPilot<T>(Entity<AltMechComponent> ent, ref T args) where T : struct
    {
        if (ent.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(pilot, ref ev);

        args = ev.Args;
    }

    protected void RelayToMech<T>(Entity<AltMechPilotComponent> ent, ref T args) where T : class
    {
        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(ent.Comp.Mech, ref ev);

        args = ev.Args;
    }

    protected void RelayRefToMech<T>(Entity<AltMechPilotComponent> ent, ref T args) where T : struct
    {
        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(ent.Comp.Mech, ref ev);

        args = ev.Args;
    }

    protected void RelayRefToParts<T>(Entity<AltMechComponent> ent, ref T args) where T : struct
    {
        var ev = new MechPartRelayedEvent<T>(args);

        foreach (var slot in ent.Comp.ContainerDict)
        {
            if (slot.Value.ContainedEntity == null)
                continue;

            RaiseLocalEvent((EntityUid)slot.Value.ContainedEntity, ref ev);
        }

        args = ev.Args;
    }

    protected void RelayRefToEquipment<T>(Entity<AltMechComponent> ent, ref T args) where T : struct
    {
        var ev = new MechEquipmentRelayedEvent<T>(args);

        foreach (var equipment in ent.Comp.EquipmentContainer.ContainedEntities)
        {
            RaiseLocalEvent(equipment, ref ev);
        }

        args = ev.Args;
    }
}

public interface IMechRelayEvent
{
}

[ByRefEvent]
public record struct MechPartRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}

[ByRefEvent]
public record struct MechEquipmentRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}
