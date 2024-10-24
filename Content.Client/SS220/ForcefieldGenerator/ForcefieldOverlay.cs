// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.SS220.ForcefieldGenerator;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ForcefieldGenerator;

public sealed class ForcefieldOverlay : Overlay
{
    private EntityManager _entity;
    private SharedTransformSystem _transform;
    private IPrototypeManager _prototype = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _shader_unshaded;

    public ForcefieldOverlay(EntityManager entMan, IPrototypeManager protoMan)
    {
        _entity = entMan;
        _transform = entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _prototype = protoMan;
        _shader_unshaded = _prototype.Index<ShaderPrototype>("unshaded").InstanceUnique();
        _shader = _prototype.Index<ShaderPrototype>("Stealth").InstanceUnique();

        ZIndex = (int) Shared.DrawDepth.DrawDepth.Overdoors;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var queryEnum = _entity.EntityQueryEnumerator<ForcefieldSS220Component>();

        while (queryEnum.MoveNext(out var uid, out var fieldComp))
        {
            if (fieldComp.Generator is not { } generator || !_entity.EntityExists(generator))
            {
                continue;
            }

            if (!_entity.TryGetComponent<ForcefieldGeneratorSS220Component>(generator, out var generatorComp))
            {
                continue;
            }

            var boxToRender = new Box2(
                new Vector2(-generatorComp.FieldLength / 2, -generatorComp.FieldThickness / 2),
                new Vector2(generatorComp.FieldLength / 2, generatorComp.FieldThickness / 2)
            );

            var (position, rotation) = _transform.GetWorldPositionRotation(uid);

            var reference = args.Viewport.WorldToLocal(position);
            reference.X = -reference.X;

            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _shader.SetParameter("reference", reference);
            var finalVisibility = Math.Clamp(generatorComp.FieldVisibility, -1f, 1f);
            _shader.SetParameter("visibility", finalVisibility);

            handle.SetTransform(position, rotation);
            handle.UseShader(_shader);
            handle.DrawRect(boxToRender, generatorComp.FieldColor);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
