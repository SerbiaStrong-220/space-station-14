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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Content.Shared.SS220.TTS;

public interface IReadOnlyTtsVoicePreferences : IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>>
{
    int Count { get; }

    ProtoId<TtsVoicePrototype> this[TtsProvider key] { get; }

    bool TryGetValue(TtsProvider provider, out ProtoId<TtsVoicePrototype> voice);

    bool ContainsKey(TtsProvider provider);

    TtsVoicePreferences Clone();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class TtsVoicePreferences : IReadOnlyTtsVoicePreferences, ISerializationHooks
{
    public int Count => _keys.Count;

    private readonly Dictionary<TtsProvider, ProtoId<TtsVoicePrototype>> _dict = [];
    private readonly List<TtsProvider> _keys = [];

    private const char StringPairsDivider = ',';
    private const char StringPairDataDivider = '|';
    private const char StringPairDataOpen = '(';
    private const char StringPairDataClose = ')';
    private const string StringInvalidData = "invalid";

    private static ISawmill Sawmill => Logger.GetSawmill(nameof(TtsVoicePreferences));

    public ProtoId<TtsVoicePrototype> this[TtsProvider key]
    {
        get => _dict[key];
        set
        {
            if (!ContainsKey(key))
            {
                Add(key, value);
                return;
            }

            _dict[key] = value;
        }
    }

    public KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>> this[int index]
    {
        get
        {
            var provider = _keys[index];
            return new KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>(provider, _dict[provider]);
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            var provider = value.Key;
            var voiceId = value.Value;

            if (!ContainsKey(provider))
            {
                Insert(index, provider, voiceId);
                return;
            }

            SetPosition(provider, index);
            _dict[provider] = voiceId;
        }
    }

    public bool Add(TtsProvider provider, ProtoId<TtsVoicePrototype> voice)
    {
        if (!InternalAdd(provider, voice))
            return false;

        _keys.Add(provider);
        return true;
    }

    public bool Insert(int index, TtsProvider provider, ProtoId<TtsVoicePrototype> voice)
    {
        if (index < 0)
            return false;

        if (index >= _keys.Count)
            return Add(provider, voice);

        if (!InternalAdd(provider, voice))
            return false;

        _keys.Insert(index, provider);
        return true;
    }

    public bool SetPosition(TtsProvider provider, int index)
    {
        if (index < 0)
            return false;

        var oldIdx = _keys.FindIndex(x => x == provider);
        if (oldIdx == -1 || oldIdx == index)
            return false;

        DebugTools.Assert(_dict.ContainsKey(provider));

        _keys.Remove(provider);
        if (_keys.Count >= index)
            _keys.Add(provider);
        else
            _keys.Insert(index, provider);

        return true;
    }

    public bool Remove(int index)
    {
        return Remove(index, out _);
    }

    public bool Remove(int index, out ProtoId<TtsVoicePrototype> voice)
    {
        voice = default;
        if (_keys.Count > index - 1)
            return false;

        return Remove(_keys[index], out voice);
    }

    public bool Remove(TtsProvider provider)
    {
        return Remove(provider, out _);
    }

    public bool Remove(TtsProvider provider, out ProtoId<TtsVoicePrototype> voice)
    {
        if (!_dict.Remove(provider, out voice))
            return false;

        DebugTools.Assert(_keys.Contains(provider));
        _keys.Remove(provider);

        return true;
    }

    public bool TryGetValue(TtsProvider provider, out ProtoId<TtsVoicePrototype> voice)
    {
        return _dict.TryGetValue(provider, out voice);
    }

    private bool InternalAdd(TtsProvider provider, ProtoId<TtsVoicePrototype> voice)
    {
        if (!_dict.TryAdd(provider, voice))
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
        DebugTools.Assert(_dict.ContainsKey(provider) == _keys.Contains(provider));
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

    public void MergeWith(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> pairs, bool hard = false)
    {
        if (hard)
            HardMergeWith(pairs);
        else
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
            Sawmill.Error($"Key mismatch: missing [{string.Join(",", missing)}], extra [{string.Join(",", extra)}]");
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < _keys.Count; i++)
        {
            if (i != 0)
                sb.Append(StringPairsDivider);

            var provider = _keys[i];

            DebugTools.Assert(_dict.ContainsKey(provider));
            if (!_dict.TryGetValue(provider, out var proto))
                proto = StringInvalidData;

            sb.Append(StringPairDataOpen);
            sb.AppendJoin(StringPairDataDivider, new[] { provider.ToString(), proto.Id });
            sb.Append(StringPairDataClose);
        }

        return sb.ToString();
    }

    public static bool TryParse(string input, [NotNullWhen(true)] out TtsVoicePreferences? result)
    {
        result = null;
        if (string.IsNullOrEmpty(input))
            return false;

        var pairs = input.Split(StringPairsDivider);
        foreach (var pair in pairs)
        {
            var sanitizedPair = pair
                .TrimStart(StringPairDataOpen)
                .TrimEnd(StringPairDataClose);

            var data = sanitizedPair.Split(StringPairDataDivider);
            if (data.Length != 2)
                continue;

            if (!Enum.TryParse<TtsProvider>(data[0], out var provider))
                continue;

            var voice = data[1];
            if (voice == StringInvalidData)
                continue;

            result ??= [];
            result.Add(provider, voice);
        }

        return result != null && result.Count > 0;
    }
}

public sealed class TtsVoicePreferencesSerializer : ITypeSerializer<TtsVoicePreferences, MappingDataNode>, ITypeCopier<TtsVoicePreferences>
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

    public void CopyTo(ISerializationManager serializationManager,
        TtsVoicePreferences source,
        ref TtsVoicePreferences target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target.Clear();
        target.MergeWith(source);
    }
}
