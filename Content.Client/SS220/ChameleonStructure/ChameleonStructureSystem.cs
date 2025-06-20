// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.IconSmoothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Polymorph.Components;
using Content.Shared.SS220.ChameleonStructure;
using Robust.Client.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Client.SS220.ChameleonStructure;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonStructureSystem : SharedChameleonStructureSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonStructureComponent, AfterAutoHandleStateEvent>(HandleState);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            PrepareAllVariants();
    }

    private void HandleState(Entity<ChameleonStructureComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    protected override void UpdateSprite(EntityUid ent, EntityPrototype proto)
    {
        base.UpdateSprite(ent, proto);

        //var xform = Transform(ent);

        //var clone = Spawn(proto.ID, xform.Coordinates);

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        if (!TryComp(ent, out IconSmoothComponent? smooth))
            return;

        if (smooth is null)
            return;

        /*
        if (!TryComp(clone, out SpriteComponent? otherSprite))
            return;
        */

        if (!proto.TryGetComponent(out SpriteComponent? otherSprite, _factory))
            return;

        if (otherSprite is null)
            return;

        if (!proto.TryGetComponent(out IconSmoothComponent? OtherSmooth, _factory))
            return;

        if (OtherSmooth is null)
            return;

        //var dragSprite = Comp<SpriteComponent>(otherSprite.Value);

        //_sprite.CopySprite((clone, otherSprite), (ent, sprite));


        smooth.StateBase = OtherSmooth.StateBase;
        sprite.CopyFrom(otherSprite);

        //_sprite.SetBaseRsi((ent, sprite), otherSprite.BaseRSI);//that was the last chance


        Dirty(ent, sprite);
    }
}
