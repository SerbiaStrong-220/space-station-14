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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggCocoonComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultYoggWeaponComponent, EntGotRemovedFromContainerMessage>(OnRemove);
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
        ent.Comp.BeforeCocooningTime = _timing.CurTime + ent.Comp.CocooningCooldown;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggWeaponComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (comp.BeforeCocooningTime is null)
                continue;

            if (_timing.CurTime < comp.BeforeCocooningTime)
                continue;

            if (!TryComp<CultYoggCorruptedComponent>(ent, out var corruptComp))
                return;

            var coords = Transform(ent).Coordinates;
            var newEnt = Spawn(comp.Item, coords);

            var corrComp = EnsureComp<CultYoggCorruptedComponent>(newEnt);
            corrComp.SoftDeletedOriginalEntity = corruptComp.SoftDeletedOriginalEntity;
            corrComp.Recipe = corruptComp.Recipe;

            EntityManager.DeleteEntity(ent);
        }
    }
}
