// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
        SubscribeLocalEvent<ChameleonStructureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonStructureComponent, AfterAutoHandleStateEvent>(HandleState);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnMapInit(Entity<ChameleonStructureComponent> ent, ref MapInitEvent args)
    {
       // SetSelectedPrototype(ent, ent.Comp.Default, true);
    }

    private void OnSelected(Entity<ChameleonStructureComponent> ent, ChameleonStructurePrototypeSelectedMessage args)
    {
        SetSelectedPrototype(ent, args.SelectedId);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            PrepareAllVariants();
    }

    public void SetSelectedPrototype(Entity<ChameleonStructureComponent> ent, string? protoId, bool forceUpdate = false)
    {
        // check that wasn't already selected
        // forceUpdate on component init ignores this check
        if (ent.Comp.Default == protoId && !forceUpdate)
            return;

        // make sure that it is valid change
        if (string.IsNullOrEmpty(protoId) || !_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        if (!IsValidTarget(proto, ent.Comp.RequireTag))
            return;

        ent.Comp.Default = protoId;

        //UpdateIdentityBlocker(uid, component, proto);
        UpdateVisuals(ent);
        UpdateUi(ent);
        Dirty(ent, ent.Comp);
    }

    private void UpdateUi(Entity<ChameleonStructureComponent> ent)
    {
        var state = new ChameleonStructureBoundUserInterfaceState(ent.Comp.Default, ent.Comp.RequireTag);
        UI.SetUiState(ent.Owner, ChameleonStructureUiKey.Key, state);
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

        /*
        if (!TryComp(clone, out SpriteComponent? otherSprite))
            return;
        */

        if (!proto.TryGetComponent(out SpriteComponent? otherSprite, _factory))
            return;

        if (otherSprite is null)
            return;

        //var dragSprite = Comp<SpriteComponent>(otherSprite.Value);

        /*
        var despawn = EnsureComp<TimedDespawnComponent>(clone);
        despawn.Lifetime = 0.01f;
        _transformSystem.SetLocalRotationNoLerp(clone, Angle.FromDegrees(180.0f));
        */
        //_sprite.CopySprite((clone, otherSprite), (ent, sprite));

        //_sprite.CopySprite((ent, sprite), (clone, otherSprite));


        //sprite.CopyFrom(otherSprite);
        //_sprite.QueueUpdateIsInert((ent, otherSprite));

        //_sprite.SetBaseRsi((ent, sprite), otherSprite.BaseRSI);//that was the last chance

        //Dirty(ent, sprite);
    }
}
