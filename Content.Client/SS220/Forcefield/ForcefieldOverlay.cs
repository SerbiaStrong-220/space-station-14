// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Shared.SS220.Forcefield.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Forcefield;

public sealed class ForcefieldOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _shader_unshaded;

    public ForcefieldOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entity.System<TransformSystem>();
        _shader_unshaded = _prototype.Index<ShaderPrototype>("unshaded").InstanceUnique();
        _shader = _prototype.Index<ShaderPrototype>("Stealth").InstanceUnique();

        ZIndex = (int) Shared.DrawDepth.DrawDepth.Overdoors;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var player = _player.LocalEntity;
        if (player == null)
            return;

        var playerMap = _transform.GetMapId(player.Value);

        var handle = args.WorldHandle;
        var query = _entity.EntityQueryEnumerator<ForcefieldComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var fieldMap = _transform.GetMapId(uid);
            if (fieldMap != playerMap)
                continue;

            var verts = comp.Params.Shape.GetTrianglesVerts();
            if (verts.Count <= 0)
                continue;

            var (pos, rot) = _transform.GetWorldPositionRotation(uid);

            var reference = args.Viewport.WorldToLocal(pos);
            reference.X = -reference.X;

            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _shader.SetParameter("reference", reference);
            var finalVisibility = Math.Clamp(comp.Params.Visibility, -1f, 1f);
            _shader.SetParameter("visibility", finalVisibility);

            handle.SetTransform(pos, rot);
            handle.UseShader(_shader);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, verts.ToList(), Color.SkyBlue);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}
