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
public sealed class TTSVoicePreferences : IEnumerable<KeyValuePair<TtsProvider, ProtoId<TTSVoicePrototype>>>
{
    [NonSerialized]
    private readonly OrderedDictionary<TtsProvider, ProtoId<TTSVoicePrototype>> _dict = [];

    public ProtoId<TTSVoicePrototype> this[TtsProvider key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public bool Add(TtsProvider provider, ProtoId<TTSVoicePrototype> protoId)
    {
        return _dict.TryAdd(provider, protoId);
    }

    public bool Insert(int index, TtsProvider provider, ProtoId<TTSVoicePrototype> protoId)
    {
        if (_dict.ContainsKey(provider))
            return false;

        _dict.Insert(index, provider, protoId);
        return true;
    }

    public IEnumerator<KeyValuePair<TtsProvider, ProtoId<TTSVoicePrototype>>> GetEnumerator()
    {
        foreach (var pair in _dict)
            yield return pair;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[TypeSerializer]
public sealed class TTSVoicePreferencesSerializer : ITypeSerializer<TTSVoicePreferences, MappingDataNode>
{
    private readonly ProtoIdSerializer<TTSVoicePrototype> _protoIdSerializer = new();

    public TTSVoicePreferences Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TTSVoicePreferences>? instanceProvider = null)
    {
        var pref = new TTSVoicePreferences();
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
                    protoMan.TryIndex<TTSVoicePrototype>(value.Value, out var ttsVoice) &&
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
        TTSVoicePreferences value,
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
