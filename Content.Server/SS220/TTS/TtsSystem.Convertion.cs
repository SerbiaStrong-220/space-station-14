using Content.Shared.SS220.TTS.Prototypes;
using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private readonly Dictionary<TtsProvider, TtsProviderHandler> _providerHandlers = [];

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, TtsKind kind)
    {
        if (!TryGetAvailableVoice(DefaultVoicePreferences, out var voice))
            return null;

        return await ConvertTextToSpeech(text, voice.Provider, voice.Speaker, kind);
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

            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            return await handler.ConvertTextToSpeech(handler.PrepareText(textSanitized, kind), speaker, kind);
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

        protected static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SS220-SS14");
            return client;
        }

        protected abstract TtsProvider Provider { get; }

        private string? _providerLabel;
        private string ProviderMetricLabel => _providerLabel ??= Provider.ToString().ToLowerInvariant();

        protected void ObserveWanted() => WantedCount.WithLabels(ProviderMetricLabel).Inc();

        protected void ObserveReused() => ReusedCount.WithLabels(ProviderMetricLabel).Inc();

        protected void ObserveRequest(string result, DateTime startTime)
        {
            RequestTimings.WithLabels(ProviderMetricLabel, result).Observe((DateTime.UtcNow - startTime).TotalSeconds);
        }

        public TtsProviderHandler(TtsSystem ttsSystem, IConfigurationManager cfg)
        {
            ConfigurationManager = cfg;
            TtsSystem = ttsSystem;

            Log = Logger.GetSawmill(ttsSystem.Log.Name + SawmillName);
        }

        /// <summary>
        /// Converts the sanitized text into the format expected by the provider API. Plain text by default.
        /// </summary>
        public virtual string PrepareText(string sanitizedText, TtsKind kind) => sanitizedText;

        public abstract Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, string speaker, TtsKind kind);

        public virtual void ClearCache()
        {
            Cache.Clear();
        }
    }
}
