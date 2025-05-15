// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChameleonStructure;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Client.SS220.ChameleonStructure.UI;

namespace Content.Client.SS220.ChameleonStructure;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonStructureSystem : SharedChameleonStructureSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonStructureComponent, AfterAutoHandleStateEvent>(HandleState);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
    }

    private void HandleState(Entity<ChameleonStructureComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    protected override void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
        base.UpdateSprite(uid, proto);
        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite, _factory))
        {
            sprite.CopyFrom(otherSprite);
        }
    }
}
