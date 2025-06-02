// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Content.Shared.SS220.CultYogg.Corruption;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Corruption;

public sealed class CultYoggCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggCocoonComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultYoggWeaponComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<CultYoggWeaponComponent, EntGotInsertedIntoContainerMessage>(OnInsert);

    }
    private void OnUseInHand(Entity<CultYoggCocoonComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var coords = Transform(args.User).Coordinates;
        var newEnt = Spawn(ent.Comp.Item, coords);

        if (TryComp<CultYoggCorruptedComponent>(ent, out var corruptComp))
        {
            var comp = EnsureComp<CultYoggCorruptedComponent>(newEnt);
            comp.SoftDeletedOriginalEntity = corruptComp.SoftDeletedOriginalEntity;
            comp.Recipe = corruptComp.Recipe;
        }

        EntityManager.DeleteEntity(ent);
        _hands.PickupOrDrop(args.User, newEnt);
        if (ent.Comp.Sound != null)
        {
            // The entity is often deleted, so play the sound at its position rather than parenting
            //var coordinates = Transform(ent).Coordinates;
            _audio.PlayPredicted(ent.Comp.Sound, args.User, args.User);
        }

        args.Handled = true;
    }
    private void OnRemove(Entity<CultYoggWeaponComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (_container.IsEntityOrParentInContainer(ent))
            return;

        ent.Comp.BeforeCocooningTime = _timing.CurTime + ent.Comp.CocooningCooldown;
    }

    private void OnInsert(Entity<CultYoggWeaponComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        ent.Comp.BeforeCocooningTime = null;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggWeaponComponent, CultYoggCorruptedComponent>();
        while (query.MoveNext(out var ent, out var comp, out var corruptComp))
        {
            if (comp.BeforeCocooningTime is null)
                continue;

            if (_timing.CurTime < comp.BeforeCocooningTime)
                continue;

            var coords = Transform(ent).Coordinates;
            var newEnt = Spawn(comp.Item, coords);

            var corrComp = EnsureComp<CultYoggCorruptedComponent>(newEnt);
            corrComp.SoftDeletedOriginalEntity = corruptComp.SoftDeletedOriginalEntity;
            corrComp.Recipe = corruptComp.Recipe;

            EntityManager.DeleteEntity(ent);
        }
    }
}
