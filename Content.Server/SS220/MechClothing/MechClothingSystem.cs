using System.Linq;
using Content.Server.Interaction;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
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
    /// <inheritdoc/>
    public override void Initialize()
    {

        SubscribeLocalEvent<MechClothingComponent, MechClothingGrabEvent>(OnInteract);
        SubscribeLocalEvent<MechClothingComponent, ComponentStartup>(OnStartUp);
        SubscribeLocalEvent<MechClothingComponent, GrabberDoAfterEvent>(OnMechGrab);
    }

    private void OnStartUp(Entity<MechClothingComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ItemContainer = _container.EnsureContainer<Container>(ent.Owner, "item-container");
    }


    private void OnInteract(Entity<MechClothingComponent> ent, ref MechClothingGrabEvent args)
    {

        if (args.Handled)
            return;

        var target = args.Target;

        if (args.Target == args.Performer || ent.Comp.DoAfter != null)
            return;

        if (TryComp<PhysicsComponent>(target, out var physics) && physics.BodyType == BodyType.Static ||
            HasComp<WallMountComponent>(target) ||
            HasComp<MobStateComponent>(target) || HasComp<MechComponent>(target))
        {
            return;
        }

        if (Transform(target).Anchored)
            return;

        if (ent.Comp.ItemContainer.ContainedEntities.Count >= ent.Comp.MaxContents)
            return;

        if (!TryComp<MechComponent>(ent.Comp.MechUid, out var mech) || mech.PilotSlot.ContainedEntity == target)
            return;

        if (mech.Energy + ent.Comp.GrabEnergyDelta < 0)
            return;

        if (!_interaction.InRangeUnobstructed(args.Performer, target))
            return;

        args.Handled = true;
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.GrabSound, ent.Owner)?.Entity;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.Performer, ent.Comp.GrabDelay, new GrabberDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);


    }

    private void OnMechGrab(Entity<MechClothingComponent> ent, ref GrabberDoAfterEvent args)
     {
         if (!TryComp<MechEquipmentComponent>(ent.Comp.CurrentEquipmentUid, out var equipmentComponent) || equipmentComponent.EquipmentOwner == null)
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
