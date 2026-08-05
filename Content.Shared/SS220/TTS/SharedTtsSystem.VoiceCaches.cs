using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.SS220.TTS;

public partial class SharedTtsSystem
{
    private readonly CategoryVoicesCache _categoryVoicesCache = new();
    private readonly ProviderVoicesCache _providerVoicesCache = new();

    public static readonly ProtoId<TtsVoiceCategoryPrototype> MiscVoiceCategoryId = "Misc";
    private TtsVoiceCategoryPrototype _miscVoiceCategory = default!;

    private void InitializeVoiceCaches()
    {
        _miscVoiceCategory = _proto.Index(MiscVoiceCategoryId);
        BuildVoiceCaches();
    }

    public IEnumerable<TtsVoicePrototype> EnumerateCategoryVoices(TtsVoiceCategoryPrototype proto)
    {
        if (!_categoryVoicesCache.TryGetValue(proto, out var voices))
            yield break;

        foreach (var voice in voices)
            yield return voice;
    }

    public IEnumerable<TtsVoicePrototype> EnumerateProviderVoices(TtsProvider provider)
    {
        if (!_providerVoicesCache.TryGetValue(provider, out var categoryCache))
            yield break;

        var returnedProtos = new HashSet<TtsVoicePrototype>();
        foreach (var voice in categoryCache.Values.SelectMany(x => x))
        {
            if (!returnedProtos.Add(voice))
                continue;

            yield return voice;
        }
    }

    public IEnumerable<KeyValuePair<TtsVoiceCategoryPrototype, List<TtsVoicePrototype>>> EnumerateProviderVoiceCategories(TtsProvider provider)
    {
        if (!_providerVoicesCache.TryGetValue(provider, out var categoryCache))
            yield break;

        foreach (var (category, voices) in categoryCache)
            yield return new KeyValuePair<TtsVoiceCategoryPrototype, List<TtsVoicePrototype>>(category, voices);
    }

    private void BuildVoiceCaches()
    {
        _providerVoicesCache.Clear();
        _categoryVoicesCache.Clear();

        foreach (var proto in _proto.EnumeratePrototypes<TtsVoicePrototype>())
        {
            var providerCategoryCache = _providerVoicesCache.GetOrNew(proto.Provider);

            if (proto.Categories.Count == 0)
            {
                AddProtoInCategoryCaches(_miscVoiceCategory);
                continue;
            }

            var addedInAnyCategory = false;
            foreach (var categoryId in proto.Categories)
            {
                if (!_proto.TryIndex(categoryId, out var category))
                    continue;

                AddProtoInCategoryCaches(category);
                addedInAnyCategory = true;
            }

            if (!addedInAnyCategory)
                AddProtoInCategoryCaches(_miscVoiceCategory);

            void AddProtoInCategoryCaches(TtsVoiceCategoryPrototype category)
            {
                providerCategoryCache.GetOrNew(category).Add(proto);
                _categoryVoicesCache.GetOrNew(category).Add(proto);
            }
        }
    }

    private sealed class ProviderVoicesCache : Dictionary<TtsProvider, CategoryVoicesCache> { }

    private sealed class CategoryVoicesCache : Dictionary<TtsVoiceCategoryPrototype, List<TtsVoicePrototype>> { }
}
