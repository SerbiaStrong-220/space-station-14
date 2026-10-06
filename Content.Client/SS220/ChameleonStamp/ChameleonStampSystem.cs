// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChameleonStamp;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ChameleonStamp;

public sealed partial class ChameleonStampSystem : SharedChameleonStampSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStampComponent, AfterAutoHandleStateEvent>(HandleState);
    }

    private void HandleState(Entity<ChameleonStampComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    protected override void UpdateSprite(Entity<ChameleonStampComponent> ent, EntityPrototype proto)
    {
        base.UpdateSprite(ent, proto);

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var clone = Spawn(proto.ID, Transform(ent).Coordinates);

        if (TryComp<SpriteComponent>(clone, out var cloneSprite))
            _sprite.CopySprite((clone, cloneSprite), (ent, sprite));

        Del(clone);
    }
}