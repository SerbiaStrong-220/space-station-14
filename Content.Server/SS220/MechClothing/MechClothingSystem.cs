// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Interaction;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Wall;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Content.Shared.SS220.MechClothing;

namespace Content.Server.SS220.MechClothing;

/// <summary>
/// This handles placing containers in claw when the player uses an action, copies part of the logic MechGrabberSystem
/// </summary>
public sealed class MechClothingSystem : EntitySystem
{
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechClothingComponent, MechClothingGrabEvent>(OnInteract);
        SubscribeLocalEvent<MechClothingComponent, ComponentStartup>(OnStartUp);
        SubscribeLocalEvent<MechClothingComponent, GrabberDoAfterEvent>(OnMechGrab);
        SubscribeLocalEvent<MechClothingComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartUp(Entity<MechClothingComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ItemContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerName);
    }

    private void OnShutdown(Entity<MechClothingComponent> ent, ref ComponentShutdown args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerName, out var container))
            return;

        _container.ShutdownContainer(container);
    }

    private void OnInteract(Entity<MechClothingComponent> ent, ref MechClothingGrabEvent args)
    {
        if (!ent.Comp.CurrentEquipmentUid.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("mech-no-equipment-selected"),ent.Owner,ent.Owner);
            return;
        }

        if (args.Handled)
            return;

        if (args.Target == args.Performer || ent.Comp.DoAfter != null)
            return;

        if (TryComp<PhysicsComponent>(args.Target, out var physics) && physics.BodyType == BodyType.Static ||
            HasComp<WallMountComponent>(args.Target) ||
            HasComp<MobStateComponent>(args.Target) ||
            HasComp<MechComponent>(args.Target))
            return;

        if (Transform(args.Target).Anchored)
            return;

        if(!TryComp<MechGrabberComponent>(ent.Comp.CurrentEquipmentUid, out var grabberComp))
            return;

        if (grabberComp.ItemContainer.ContainedEntities.Count >= grabberComp.MaxContents)
            return;

        if (!TryComp<MechComponent>(ent.Comp.MechUid, out var mech))
            return;

        if (mech.Energy + ent.Comp.GrabEnergyDelta < 0)
        {
            _popup.PopupEntity(Loc.GetString("mech-not-enough-energy"), ent.Owner, ent.Owner);
            return;
        }

        if (!_interaction.InRangeUnobstructed(args.Performer, args.Target))
            return;

        args.Handled = true;
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.GrabSound, ent.Owner)?.Entity;
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.Performer,
            ent.Comp.GrabDelay,
            new GrabberDoAfterEvent(),
            ent.Owner,
            target: args.Target,
            used: ent.Owner)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
    }

    private void OnMechGrab(Entity<MechClothingComponent> ent, ref GrabberDoAfterEvent args)
     {
         if (!TryComp<MechEquipmentComponent>(ent.Comp.CurrentEquipmentUid, out var equipmentComponent) ||
             equipmentComponent.EquipmentOwner == null)
             return;

         ent.Comp.DoAfter = null;

        if (args.Cancelled)
        {
            ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        if (!_mech.TryChangeEnergy(equipmentComponent.EquipmentOwner.Value, ent.Comp.GrabEnergyDelta))
            return;

        if(!TryComp<MechGrabberComponent>(ent.Comp.CurrentEquipmentUid, out var mechGrabberComp))
            return;

        _container.Insert(args.Args.Target.Value, mechGrabberComp.ItemContainer);
        _mech.UpdateUserInterface(equipmentComponent.EquipmentOwner.Value);

        args.Handled = true;
    }
}
