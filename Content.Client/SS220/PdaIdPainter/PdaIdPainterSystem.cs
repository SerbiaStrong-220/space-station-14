using Content.Client.PDA;
using Content.Shared.SS220.PdaIdPainter;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.PdaIdPainter;

public sealed class PdaIdPainterSystem : SharedPdaIdPainterSystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaIdPainterTargetComponent, AfterAutoHandleStateEvent>(HandleState);
    }

    private void HandleState(Entity<PdaIdPainterTargetComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent.Comp.NewProto, ent.Owner);
    }

    protected override void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
        base.UpdateSprite(uid, proto);

        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite, _factory))
        {
            sprite.CopyFrom(otherSprite);
        }

        if (TryComp(uid, out IconComponent? icon)
            && proto.TryGetComponent(out IconComponent? otherIcon, _factory))
        {
            icon.Icon = otherIcon.Icon;
        }

        if (!TryComp(uid, out PdaBorderColorComponent? borderColor)
            || !proto.TryGetComponent(out PdaBorderColorComponent? otherBorderColor, _factory))
            return;

        borderColor.BorderColor = otherBorderColor.BorderColor;
        borderColor.AccentHColor = otherBorderColor.AccentHColor;
        borderColor.AccentVColor = otherBorderColor.AccentVColor;

    }
}
