// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.IconSmoothing;
using Content.Shared.SS220.ChameleonStructure;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ChameleonStructure;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonStructureSystem : SharedChameleonStructureSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IconSmoothSystem _smooth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStructureComponent, AfterAutoHandleStateEvent>(HandleState);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            UpdateData();
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

        if (!TryComp<SpriteComponent>(clone, out var cloneSprite))
            return;

        if (!TryCopySmooth(ent, clone))//Prevent errors
            return;

        _sprite.CopySprite((clone, cloneSprite), (ent, sprite));

        Del(clone);

        Dirty(ent, sprite);

        _smooth.DirtyNeighbours(ent);//requred to fix our FOV
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
