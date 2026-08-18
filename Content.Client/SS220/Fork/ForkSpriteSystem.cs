// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.SS220.Fork;

public sealed partial class ForkSpriteSystem : EntitySystem
{
    [Dependency] private IResourceCache resourceCache = default!;
    [Dependency] private SpriteSystem sprite = default!;

#if DEBUG
    private HashSet<string> _errors = new();
#endif

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteComponent, ComponentAdd>(OnSpriteAdd);
        SubscribeLocalEvent<ForkSpriteComponent, ComponentStartup>(OnForkSpriteStartup);
    }

    private void OnSpriteAdd(Entity<SpriteComponent> entity, ref ComponentAdd _)
    {
        EnsureComp<ForkSpriteComponent>(entity);
    }

    private void OnForkSpriteStartup(Entity<ForkSpriteComponent> entity, ref ComponentStartup _)
    {
        if (entity.Comp.Disabled)
            return;

        SpriteComponent? spriteComponent = null;
        if (!Resolve(entity, ref spriteComponent))
            return;

        if (spriteComponent.BaseRSI is not { } spriteRsi)
            return;

        if (spriteRsi.Path.CanonPath.Contains(ForkSpriteComponent.ForkFolder))
            return;

        var rsiPath = SpriteSpecifierSerializer.TextureRoot / ForkSpriteComponent.ForkFolder / spriteRsi.Path.RelativeTo(SpriteSpecifierSerializer.TextureRoot);
        if (resourceCache.TryGetResource(rsiPath, out RSIResource? resource))
        {
            var allLayersExists = true;
            foreach (var layer in spriteComponent.AllLayers)
            {
                if (!resource.RSI.TryGetState(layer.RsiState.Name, out var _))
                    allLayersExists = false;
            }

            // not logged to remove white-noise
            if (!allLayersExists)
                return;

            sprite.SetBaseRsi((entity.Owner, spriteComponent), resource.RSI);
        }
        else
        {
#if DEBUG
            _errors.Add($"RSI - {spriteRsi.Path} don't have fork sprite!");
#endif
        }
    }
}
