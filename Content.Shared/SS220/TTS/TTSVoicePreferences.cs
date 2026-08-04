using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using System.Collections;
using System.Linq;

namespace Content.Shared.SS220.TTS;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class TtsVoicePreferences : IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>>, ISerializationHooks
{
    private readonly Dictionary<TtsProvider, ProtoId<TtsVoicePrototype>> _dict = [];
    private readonly List<TtsProvider> _keys = [];

    public ProtoId<TtsVoicePrototype> this[TtsProvider key]
    {
        get => _dict[key];
        set
        {
            if (!_dict.ContainsKey(key))
            {
                Add(key, value);
                return;
            }

            _dict[key] = value;
        }
    }

    public bool Add(TtsProvider provider, ProtoId<TtsVoicePrototype> protoId)
    {
        if (!InternalAdd(provider, protoId))
            return false;

        _keys.Add(provider);
        return true;
    }

    public bool Insert(int index, TtsProvider provider, ProtoId<TtsVoicePrototype> protoId)
    {
        if (!InternalAdd(provider, protoId))
            return false;

        _keys.Insert(index, provider);
        return true;
    }

    private bool InternalAdd(TtsProvider provider, ProtoId<TtsVoicePrototype> protoId)
    {
        if (!_dict.TryAdd(provider, protoId))
            return false;

        DebugTools.Assert(!_keys.Contains(provider));
        return true;
    }

    public void Clear()
    {
        _dict.Clear();
        _keys.Clear();
    }

    public bool ContainsKey(TtsProvider provider)
    {
        return _dict.ContainsKey(provider);
    }

    public IEnumerator<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> GetEnumerator()
    {
        foreach (var key in _keys)
            yield return new KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>(key, _dict[key]);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void SetValuesFrom(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> pairs)
    {
        Clear();
        SoftMergeWith(pairs);
    }

    public void HardMergeWith(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> pairs)
    {
        foreach (var (key, value) in pairs)
            this[key] = value;
    }

    public void SoftMergeWith(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> pairs)
    {
        foreach (var (key, value) in pairs)
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

        preferences.SoftMergeWith(pairs);
        return preferences;
    }

    void ISerializationHooks.AfterDeserialization()
    {
        var missing = _keys.Except(_dict.Keys).ToList();
        var extra = _dict.Keys.Except(_keys).ToList();

        if (missing.Count != 0 || extra.Count != 0)
            Logger.GetSawmill(nameof(TtsVoicePreferences)).Error($"Key mismatch: missing [{string.Join(",", missing)}], extra [{string.Join(",", extra)}]");
    }
}

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
