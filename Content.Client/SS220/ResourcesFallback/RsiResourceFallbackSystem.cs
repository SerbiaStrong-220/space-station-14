using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client.SS220.ResourcesFallback
{
    public sealed class RsiResourceFallbackSystem : EntitySystem
    {
        [Dependency] private IResourceCache _resourceCache = default!;

        private readonly ResPath _fallbackRsiPath = new("/Textures/SS220/Misc/Errors/error-curse220.rsi");

        public override void Initialize()
        {
            var fallbackResource = _resourceCache.GetResource<RSIResource>(_fallbackRsiPath);
            _resourceCache.CacheResource("/Textures/error.rsi", fallbackResource);
        }
    }
}
