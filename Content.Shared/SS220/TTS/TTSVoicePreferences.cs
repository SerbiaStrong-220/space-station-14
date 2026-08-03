using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using System.Collections;

namespace Content.Shared.SS220.TTS;

[Serializable]
public sealed class TtsVoicePreferences() : IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>>
{
    [NonSerialized]
    private readonly OrderedDictionary<TtsProvider, ProtoId<TtsVoicePrototype>> _dict = [];

    public ProtoId<TtsVoicePrototype> this[TtsProvider key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public bool Add(TtsProvider provider, ProtoId<TtsVoicePrototype> protoId)
    {
        return _dict.TryAdd(provider, protoId);
    }

    public bool Insert(int index, TtsProvider provider, ProtoId<TtsVoicePrototype> protoId)
    {
        if (_dict.ContainsKey(provider))
            return false;

        _dict.Insert(index, provider, protoId);
        return true;
    }

    public bool ContainsKey(TtsProvider provider)
    {
        return _dict.ContainsKey(provider);
    }

    public IEnumerator<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> GetEnumerator()
    {
        foreach (var pair in _dict)
            yield return pair;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void HardMergeWith(TtsVoicePreferences other)
    {
        foreach (var (key, value) in other)
            this[key] = value;
    }

    public void SoftMergeWith(TtsVoicePreferences other)
    {
        foreach (var (key, value) in other)
            Add(key, value);
    }

    public TtsVoicePreferences Clone()
    {
        return FromEnumerable(this);
    }

    public static TtsVoicePreferences FromDictionary(IDictionary<TtsProvider, ProtoId<TtsVoicePrototype>> dict)
    {
        return FromEnumerable(dict);
    }

    public static TtsVoicePreferences FromEnumerable(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> pairs)
    {
        var preferences = new TtsVoicePreferences();

        foreach (var (key, value) in pairs)
            preferences.Add(key, value);

        return preferences;
    }
}

[TypeSerializer]
public sealed class TTSVoicePreferencesSerializer : ITypeSerializer<TtsVoicePreferences, MappingDataNode>
{
    private readonly ProtoIdSerializer<TtsVoicePrototype> _protoIdSerializer = new();

    public TtsVoicePreferences Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TtsVoicePreferences>? instanceProvider = null)
    {
        var pref = new TtsVoicePreferences();
        foreach (var (key, data) in node.Children)
        {
            if (!Enum.TryParse<TtsProvider>(key, out var provider))
                continue;

            var value = _protoIdSerializer.Read(serializationManager, (ValueDataNode)data, dependencies, hookCtx, context);
            pref.Add(provider, value);
        }

        return pref;
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var protoMan = dependencies.Resolve<IPrototypeManager>();
        var dict = new Dictionary<ValidationNode, ValidationNode>();

        foreach (var (key, data) in node.Children)
        {
            var value = (ValueDataNode)data;
            var valueValidationNode = _protoIdSerializer.Validate(serializationManager, (ValueDataNode)data, dependencies, context);

            ValidationNode keyValidationNode;
            if (Enum.TryParse<TtsProvider>(key, out var provider))
            {
                keyValidationNode = new ValidatedValueNode(node.GetKeyNode(key));
                if (valueValidationNode is not ErrorNode &&
                    protoMan.TryIndex<TtsVoicePrototype>(value.Value, out var ttsVoice) &&
                    ttsVoice.Provider != provider)
                {
                    valueValidationNode = new ErrorNode(value,
                        $"Provider mismatch: key '{provider}' does not match prototype '{value.Value}' with provider '{ttsVoice.Provider}'");
                }
            }
            else
                keyValidationNode = new ErrorNode(node.GetKeyNode(key), $"Failed to parse Provider: {key}");

            dict.Add(keyValidationNode, valueValidationNode);
        }

        return new ValidatedMappingNode(dict);
    }

    public DataNode Write(ISerializationManager serializationManager,
        TtsVoicePreferences value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var mapping = new MappingDataNode();
        foreach (var (key, protoId) in value)
            mapping.Add(key.ToString(), _protoIdSerializer.Write(serializationManager, protoId, dependencies, alwaysWrite, context));

        return mapping;
    }
}
