// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.SS220.Overlays;

public sealed class BoxesOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private readonly ShaderInstance _shader;
    private readonly Texture _defaultTexture;

    private readonly Dictionary<Type, BoxesOverlayProvider> _providers = new();

    private BoxesOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index<ShaderPrototype>("unshaded").Instance();
        _defaultTexture = _cache.GetTexture("/Textures/Interface/Nano/square.png");
    }

    public static BoxesOverlay GetOverlay()
    {
        var overlayMng = IoCManager.Resolve<IOverlayManager>();
        if (overlayMng.TryGetOverlay<BoxesOverlay>(out var overlay))
            return overlay;
        else
        {
            var newOverlay = new BoxesOverlay();
            overlayMng.AddOverlay(newOverlay);
            return newOverlay;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;
        drawHandle.UseShader(_shader);

        foreach (var provider in _providers.Values)
        {
            if (!provider.Active)
                continue;

            foreach (var data in provider.GetBoxesDatas())
                DrawBox(args, data);
        }

        drawHandle.SetTransform(Matrix3x2.Identity);
        drawHandle.UseShader(_shader);
    }

    private void DrawBox(in OverlayDrawArgs args, BoxOverlayData data)
    {
        var xform = _entManager.GetComponent<TransformComponent>(data.Parent);
        if (xform.MapID != args.MapId)
            return;

        var transform = _entManager.System<SharedTransformSystem>();
        var (_, _, worldMatrix, _) = transform.GetWorldPositionRotationMatrixWithInv(xform);

        var drawHandle = args.WorldHandle;
        drawHandle.SetTransform(worldMatrix);

        var texture = data.Texture ?? _defaultTexture;
        drawHandle.DrawTextureRect(texture, data.Box, data.Color);
    }

    public bool AddProvider<T>(T provider) where T : BoxesOverlayProvider
    {
        return _providers.TryAdd(typeof(T), provider);
    }

    public bool RemoveProvder<T>() where T : BoxesOverlayProvider
    {
        return _providers.Remove(typeof(T));
    }

    public bool HasProvider<T>() where T : BoxesOverlayProvider
    {
        return _providers.ContainsKey(typeof(T));
    }

    public bool TryGetProvider<T>([NotNullWhen(true)] out T? provider) where T : BoxesOverlayProvider
    {
        provider = null;
        if (_providers.TryGetValue(typeof(T), out var result))
        {
            provider = (T)result;
            return true;
        }
        else
            return false;
    }

    public abstract class BoxesOverlayProvider
    {
        public bool Active = false;

        public BoxesOverlayProvider()
        {
            IoCManager.InjectDependencies(this);
        }

        public abstract List<BoxOverlayData> GetBoxesDatas();
    }

    public struct BoxOverlayData(EntityUid parent, Box2? box = null, Color? color = null, Texture? texture = null)
    {
        public EntityUid Parent = parent;

        public Box2 Box = box ?? new();

        public Color Color = color ?? Color.White;

        public Texture? Texture = texture;
    }
}
