// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.GameTicking;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Commands;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : SharedTTSSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;

    #region Prometheus
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    private static readonly Counter WantedRadioCount = Metrics.CreateCounter(
        "tts_wanted_radio_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedRadioCount = Metrics.CreateCounter(
        "tts_reused_radio_count",
        "Amount of reused TTS audio from cache.");
    #endregion

    private int _maxMessageChars;
    private int _maxAnnounceMessageChars;
    private bool _isEnabled = false;

    private HashSet<ICommonSession> _sessionsNotToSend = new();

    private static float _requestTimeout = 1f;
    private const string AudioFileExtension = "ogg";

    // Kirus ToDo: перенести в датасет
    private readonly List<string> _sampleText =
    [
        "Съешь же ещё этих мягких французских булок, да выпей чаю.",
        "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
        "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
        "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
        "Учёные, тут странная аномалия в баре! Она уже съела мима!",
        "Я надеюсь что инженеры внимательно следят за сингулярностью...",
        "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
        "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
        "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
        "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
        "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
        "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
    ];

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars220.MaxCharInTTSAnnounceMessage, x => _maxAnnounceMessageChars = x, true);
        Subs.CVar(_cfg, CCVars220.MaxCharInTTSMessage, x => _maxMessageChars = x, true);
        Subs.CVar(_cfg, CCVars220.TTSEnabled, v => _isEnabled = v, true);
        Subs.CVar(_cfg, CCVars220.TTSRequestTimeout, v => _requestTimeout = v, true);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestTTSVoiceTestEvent>(OnRequestTTSVoiceTest);

        // remove if Robust PR for clientCVar subs merged
        SubscribeNetworkEvent<ReceiveTtsCVarChanged>(OnReceiveTtsCVarChanged);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        // end

        InitializeEntitySubscriptions();

        InitializeNTTS();
        InitializeSilero();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        ClearCache();
    }

    private void OnRequestTTSVoiceTest(RequestTTSVoiceTestEvent ev, EntitySessionEventArgs args)
    {
        if (!_prototypeManager.TryIndex(ev.VoiceId, out var voice))
            return;

        var text = _random.Pick(_sampleText);

        var request = new TtsVoiceTestRequest()
        {
            Voice = voice,
            Text = _random.Pick(_sampleText),
            Receivers = [args.SenderSession]
        };

        RunTaskWithTryCatch(() => HandleVoiceTestRequest(request));
    }

    private void OnReceiveTtsCVarChanged(ReceiveTtsCVarChanged msg, EntitySessionEventArgs args)
    {
        if (!msg.Value)
            _sessionsNotToSend.Add(args.SenderSession);
        else
            _sessionsNotToSend.Remove(args.SenderSession);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _sessionsNotToSend.Remove(e.Session);
    }

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, ProtoId<TTSVoicePrototype>? protoId, TtsKind kind)
    {
        if (protoId == null && !TryGetDefaultPreferredVoice(out protoId))
            return null;

        if (!_prototypeManager.TryIndex(protoId, out var proto))
            return null;

        return await ConvertTextToSpeech(text, proto, kind);
    }

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, TTSVoicePrototype voice, TtsKind kind)
    {
        return await ConvertTextToSpeech(text, voice.Provider, voice.Speaker, kind);
    }

    public async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, TtsProvider provider, string speaker, TtsKind kind)
    {
        if (!IsProviderEnabled(provider))
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

            return provider switch
            {
                TtsProvider.NTTS => await NTTSHandler.ConvertTextToSpeech(speaker, textSsml, kind),
                TtsProvider.Silero => await SileroTTSHandler.ConvertTextToSpeech(speaker, textSsml, kind),
                _ => null
            };
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            return null;
        }
    }

    public void ClearCache()
    {
        ClearCache(Enum.GetValues<TtsProvider>());
    }

    public void ClearCache(params TtsProvider[] providers)
    {
        foreach (var provider in providers)
            ClearCache(provider);
    }

    public void ClearCache(TtsProvider provider)
    {
        switch (provider)
        {
            case TtsProvider.NTTS:
                NTTSHandler.Cache.Clear();
                break;

            case TtsProvider.Silero:
                SileroTTSHandler.Cache.Clear();
                break;
        }
    }

    private static string GenerateCacheKey(string text, TtsProvider? provider = null, string? speaker = null, TtsKind? kind = null)
    {
        var sb = new StringBuilder();
        sb.Append(text);

        TryAddInfo(provider?.ToString());
        TryAddInfo(speaker);
        TryAddInfo(kind?.ToString());

        var key = sb.ToString();
        var keyData = Encoding.UTF8.GetBytes(key);
        var bytes = System.Security.Cryptography.SHA256.HashData(keyData);
        return Convert.ToHexString(bytes);

        void TryAddInfo(string? info)
        {
            if (info == null)
                return;

            sb.Append("/" + info);
        }
    }

    private static string ToQueryString(NameValueCollection nvc)
    {
        var array = (
            from key in nvc.AllKeys
            from value in nvc.GetValues(key) ?? Array.Empty<string>()
            select $"{key}={HttpUtility.UrlEncode(value)}"
            ).ToArray();

        return "?" + string.Join("&", array);
    }

    private void RunTaskWithTryCatch(Func<Task> task)
    {
        if (task == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                await task().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}\n{e.StackTrace}");
            }
        });
    }

    /// <summary>
    /// Set random voice from RandomVoicesList
    /// If RandomVoicesList is null - doesn`t set new voice
    /// </summary>
    private void SetRandomVoice(Entity<TTSComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        var protoId = entity.Comp.RandomVoicesList;

        if (protoId is null)
            return;

        entity.Comp.VoicePrototypeId = _random.Pick(_prototypeManager.Index<RandomVoicesListPrototype>(protoId).VoicesList);
    }

    private IEnumerable<ICommonSession> ToValidReceivers(IEnumerable<EntityUid> entities)
    {
        return ToValidReceivers(EntitiesToSessions(entities));
    }

    private IEnumerable<ICommonSession> ToValidReceivers(IEnumerable<ICommonSession> receivers)
    {
        return receivers.Where(IsValidReceiver);
    }

    private IEnumerable<ICommonSession> EntitiesToSessions(IEnumerable<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver))
                yield return receiver;
        }
    }

    private bool IsValidReceiver(ICommonSession receiver)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return false;

        if (receiver.Status == SessionStatus.Disconnected)
            return false;

        return true;
    }

    private sealed class TtsCache()
    {
        private readonly ConcurrentDictionary<TtsCacheKey, TtsResponse.Reference> _lookup = new();
        private readonly ConcurrentQueue<TtsCacheKey> _keysQueue = new();

        public int Limit
        {
            get => _limit;
            set => _limit = Math.Max(value, 0);
        }

        private int _limit = 1;

        public TtsCache(int limit) : this()
        {
            Limit = limit;
        }

        public void Cache(TtsCacheKey key, TtsResponse value)
        {
            var currentCount = _lookup.Count;
            while (currentCount > 0 && currentCount + 1 > Limit)
            {
                if (_keysQueue.TryDequeue(out var firstKey) && _lookup.TryRemove(firstKey, out var responce))
                    responce.Dispose();

                currentCount = _lookup.Count;
            }

            if (Limit != 0)
            {
                _lookup[key] = value.GetReference();
                _keysQueue.Enqueue(key);
            }
        }

        public bool TryGet(TtsCacheKey key, [NotNullWhen(true)] out TtsResponse.Reference? responce)
        {
            responce = null;
            if (!_lookup.TryGetValue(key, out var exist))
                return false;

            responce = exist;
            return true;
        }

        public void Clear()
        {
            _lookup.Clear();
            _keysQueue.Clear();
        }

        public void Trim()
        {
            while (_lookup.Count > Limit)
            {
                if (_keysQueue.TryDequeue(out var firstKey) && _lookup.TryRemove(firstKey, out var reuseBuffer))
                    reuseBuffer.GetReference().Dispose();
            }

        }
    }

    private struct ServerTtsMessage() : IDisposable
    {
        public required TtsResponse.Reference ResponceReference;
        public SharedTtsMetadata Metadata;
        public NetEntity? Source;

        public bool Disposed { get; private set; } = false;

        public ServerTtsMessage WithResponce(TtsResponse.Reference responceRef)
        {
            return new()
            {
                ResponceReference = responceRef.GetReference(),
                Metadata = Metadata,
                Source = Source
            };
        }

        public ServerTtsMessage WithMetadata(SharedTtsMetadata meta)
        {
            return new()
            {
                ResponceReference = ResponceReference.GetReference(),
                Metadata = meta,
                Source = Source
            };
        }

        public ServerTtsMessage WithSource(NetEntity source)
        {
            return new()
            {
                ResponceReference = ResponceReference.GetReference(),
                Metadata = Metadata,
                Source = Source
            };
        }

        public void Dispose()
        {
            ResponceReference.Dispose();
            Disposed = true;
        }
    }
}

[Virtual]
public class ReferenceCounter<T>(T value)
{
    public T Value = value;
    public int ReferenceCount { get; private set; } = 0;

    public Reference GetReference()
    {
        ReferenceCount++;
        return new(this);
    }

    protected virtual void OnReferenceDisposed()
    {
        ReferenceCount--;
    }

    public struct Reference(ReferenceCounter<T> counter) : IDisposable
    {
        private readonly ReferenceCounter<T> _counter = counter;
        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _counter.OnReferenceDisposed();
        }

        public readonly Reference GetReference()
        {
            return _counter.GetReference();
        }

        public readonly bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            value = _counter.Value;
            return _disposed && value != null;
        }
    }
}

public static class ReferenceCounterExtentions
{
    public static bool TryGetValue<T>(this ReferenceCounter<T>.Reference? reference, [NotNullWhen(true)] out T? value)
    {
        value = default;
        return reference != null && reference.Value.TryGetValue(out value);
    }
}
