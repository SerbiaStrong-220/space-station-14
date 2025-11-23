// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.IconSmoothing;
using Content.Shared.Doors.Components;
using Content.Shared.SS220.ChameleonStructure;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ChameleonStructure;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonStructureSystem : SharedChameleonStructureSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IconSmoothSystem _smooth = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStructureComponent, AfterAutoHandleStateEvent>(HandleState);
    }

    private void HandleState(Entity<ChameleonStructureComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    protected override void UpdateSprite(EntityUid ent, EntityPrototype proto)
    {
        base.UpdateSprite(ent, proto);

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var clone = Spawn(proto.ID, Transform(ent).Coordinates);

        if (TryComp<SpriteComponent>(clone, out var cloneSprite) && TryCopySmooth(ent, clone))
        {
            _sprite.CopySprite((clone, cloneSprite), (ent, sprite));

            Dirty(ent, sprite);
            _smooth.DirtyNeighbours(ent); //required to fix our FOV
        }

        if (TryComp<OccluderComponent>(clone, out var cloneOccluder) && TryComp<OccluderComponent>(ent, out var occluder))//transparence
        {
            _occluder.SetEnabled(ent, cloneOccluder.Enabled, occluder);
            Dirty(ent, occluder);

            if (TryComp<DoorComponent>(ent, out var door))//idk how to make it "good", event?
            {
                door.Occludes = cloneOccluder.Enabled;
            }
        }

        Del(clone);
    }

    private bool TryCopySmooth(EntityUid ent, EntityUid clone)//Should be optional, but idk how to do it
    {
        if (!TryComp<IconSmoothComponent>(ent, out var smooth))
            return false;

        if (!TryComp<IconSmoothComponent>(clone, out var cloneSmooth))
            return false;

        smooth.StateBase = cloneSmooth.StateBase;

        Dirty(ent, smooth);
        return true;
    }
}
