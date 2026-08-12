using Content.Shared.SS220.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private readonly Dictionary<TtsProvider, TtsProviderHandler> _providerHandlers = [];

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, ProtoId<TtsVoicePrototype>? protoId, TtsKind kind)
    {
        if (protoId == null && !TryGetDefaultPreferredVoice(out protoId))
            return null;

        if (!_prototypeManager.TryIndex(protoId, out var proto))
            return null;

        return await ConvertTextToSpeech(text, proto, kind);
    }

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, TtsVoicePrototype voice, TtsKind kind)
    {
        return await ConvertTextToSpeech(text, voice.Provider, voice.Speaker, kind);
    }

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, TtsProvider provider, string speaker, TtsKind kind)
    {
        if (!IsProviderEnabled(provider))
            return null;

        if (!TryGetProviderHandler(provider, out var handler))
            return null;

        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "")
                return null;

            var ssmlTraits = SoundTraits.RateFast;
            if (kind == TtsKind.Whisper)
                ssmlTraits |= SoundTraits.PitchVerylow;

            var textSsml = ToSsmlText(textSanitized, ssmlTraits);

            return await handler.ConvertTextToSpeech(textSsml, speaker, kind);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            return null;
        }
    }

    private void RegisterProviderHandler(TtsProvider provider, TtsProviderHandler handler)
    {
        if (_providerHandlers.ContainsKey(provider))
        {
            Log.Error($"A handler for provider {provider} already registered!");
            return;
        }

        _providerHandlers[provider] = handler;
    }

    private bool TryGetProviderHandler(TtsProvider provider, [NotNullWhen(true)] out TtsProviderHandler? handler)
    {
        return _providerHandlers.TryGetValue(provider, out handler);
    }

    private bool TryGetProviderHandler<T>(TtsProvider provider, [NotNullWhen(true)] out T? handler) where T : TtsProviderHandler
    {
        handler = null;
        if (!_providerHandlers.TryGetValue(provider, out var exist))
            return false;

        handler = (T)exist;
        return true;
    }

    private abstract class TtsProviderHandler
    {
        protected readonly TtsSystem TtsSystem;
        protected readonly IConfigurationManager ConfigurationManager;
        protected readonly ISawmill Log;

        protected readonly TtsCache Cache = new();

        protected virtual string SawmillName => GetType().Name;

        public TtsProviderHandler(TtsSystem ttsSystem, IConfigurationManager cfg)
        {
            ConfigurationManager = cfg;
            TtsSystem = ttsSystem;

            Log = Logger.GetSawmill(ttsSystem.Log.Name + SawmillName);
        }

        public abstract Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, string speaker, TtsKind kind);

        public virtual void ClearCache()
        {
            Cache.Clear();
        }
    }
}
