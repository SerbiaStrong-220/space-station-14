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

    private ShaderInstance _shader;
    private Texture _texture;

    private Dictionary<Type, BoxesDatasProvider> _providers = new();

    private BoxesOverlay() : base()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index<ShaderPrototype>("unshaded").Instance();
        _texture = _cache.GetTexture("/Textures/Interface/Nano/square.png");
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
            foreach (var data in provider.GetBoxesDatas())
                DrawBoxes(args, data);
        }

        drawHandle.SetTransform(Matrix3x2.Identity);
        drawHandle.UseShader(_shader);
    }

    private void DrawBoxes(in OverlayDrawArgs args, BoxesData data)
    {
        var xform = _entManager.GetComponent<TransformComponent>(data.Parent);
        if (xform.MapID != args.MapId)
            return;

        var transform = _entManager.System<SharedTransformSystem>();
        var (_, _, worldMatrix, _) = transform.GetWorldPositionRotationMatrixWithInv(xform);

        var drawHandle = args.WorldHandle;
        drawHandle.SetTransform(worldMatrix);
        foreach (var box in data.Boxes)
            drawHandle.DrawTextureRect(_texture, box, data.Color);
    }

    public bool AddProvider(BoxesDatasProvider provider)
    {
        return _providers.TryAdd(provider.GetType(), provider);
    }

    public bool RemoveProvider(BoxesDatasProvider provider)
    {
        return RemoveProvider(provider.GetType());
    }

    public bool RemoveProvider<T>() where T : BoxesDatasProvider
    {
        return RemoveProvider(typeof(T));
    }

    public bool RemoveProvider(Type providerType)
    {
        return _providers.Remove(providerType);
    }

    public bool HasProvider(BoxesDatasProvider provider)
    {
        return HasProvider(provider.GetType());
    }

    public bool HasProvider<T>() where T : BoxesDatasProvider
    {
        return HasProvider(typeof(T));
    }

    public bool HasProvider(Type providerType)
    {
        return _providers.ContainsKey(providerType);
    }

    public bool TryGetProvider<T>([NotNullWhen(true)] out BoxesDatasProvider? provider) where T : BoxesDatasProvider
    {
        return TryGetProvider(typeof(T), out provider);
    }

    public bool TryGetProvider(Type providerType, [NotNullWhen(true)] out BoxesDatasProvider? provider)
    {
        return _providers.TryGetValue(providerType, out provider);
    }

    public abstract class BoxesDatasProvider
    {
        public BoxesDatasProvider()
        {
            IoCManager.InjectDependencies(this);
        }

        public abstract List<BoxesData> GetBoxesDatas();
    }

    [Virtual]
    public class BoxesData(EntityUid parent)
    {
        public EntityUid Parent = parent;

        public HashSet<Box2> Boxes = new();

        public Color Color = Color.White;
    }
}
