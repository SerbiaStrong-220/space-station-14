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
/// This handles...
/// </summary>
public sealed class MechClothingSystem : EntitySystem
{
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechClothingComponent, MechClothingGrabEvent>(OnInteract);
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

        if (!TryComp<MechComponent>(args.Performer, out var mech) || mech.PilotSlot.ContainedEntity == target)
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
}
