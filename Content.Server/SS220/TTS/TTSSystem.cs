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
    [Dependency] private IServerNetManager _netManager = default!;
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

        //_netManager.RegisterNetMessage<PlayTtsMessage>();
        //_netManager.RegisterNetMessage<MsgPlayAnnounceTts>();

        _cfg.OnValueChanged(CCVars220.MaxCharInTTSAnnounceMessage, x => _maxAnnounceMessageChars = x, true);
        _cfg.OnValueChanged(CCVars220.MaxCharInTTSMessage, x => _maxMessageChars = x, true);
        _cfg.OnValueChanged(CCVars220.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCVars220.TTSRequestTimeout, v => _requestTimeout = v, true);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestTTSVoiceTestEvent>(OnRequestTTSVoiceTest);

        // remove if Robust PR for clientCVar subs merged
        SubscribeNetworkEvent<SessionSendTTSMessage>((msg, args) =>
        {
            if (!msg.Value)
                _sessionsNotToSend.Add(args.SenderSession);
            else
                _sessionsNotToSend.Remove(args.SenderSession);
        });

        _playerManager.PlayerStatusChanged += (_, x) =>
        {
            if (x.NewStatus == SessionStatus.Disconnected)
                _sessionsNotToSend.Remove(x.Session);
        };
        // end

        InitializeEntitySubscriptions();

        InitializeNTTS();
        InitializeSilero();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        ClearCache();
    }

    private async void OnRequestTTSVoiceTest(RequestTTSVoiceTestEvent ev, EntitySessionEventArgs args)
    {
        var text = _random.Pick(_sampleText);
        using var ttsResponse = await ConvertTextToSpeech(text, ev.VoiceId, TtsKind.VoiceTest);
        if (!ttsResponse.TryGetValue(out var audioData))
            return;

        SendTtsMessage(new PlayTtsMessage { AudioData = audioData }, args.SenderSession);
    }

    public async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeech(ProtoId<TTSVoicePrototype>? protoId, string text, TtsKind kind)
    {
        if (protoId == null && !TryGetDefaultPreferredVoice(out protoId))
            return default;

        if (!_prototypeManager.TryIndex(protoId, out var proto))
            return default;

        return await ConvertTextToSpeech(proto.Provider, proto.Speaker, text, kind);
    }

    public async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeech(TTSProvider provider, string speaker, string text, TtsKind kind)
    {
        if (!IsProviderEnabled(provider))
            return null;

        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return default;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            var ssmlTraits = SoundTraits.RateFast;
            if (kind == TtsKind.Whisper)
                ssmlTraits |= SoundTraits.PitchVerylow;

            var textSsml = ToSsmlText(textSanitized, ssmlTraits);

            return provider switch
            {
                TTSProvider.NTTS => await NTTSHandler.ConvertTextToSpeech(speaker, textSsml, kind),
                TTSProvider.Silero => await SileroTTSHandler.ConvertTextToSpeech(speaker, textSsml, kind),
                _ => null
            };
        }
        catch (Exception e)
        {
            // Catch TTS exceptions to prevent a server crash.
            Log.Error($"TTS System error: {e.Message}");
            return null;
        }
    }

    public void ClearCache()
    {
        ClearCache(Enum.GetValues<TTSProvider>());
    }

    public void ClearCache(params TTSProvider[] providers)
    {
        foreach (var provider in providers)
            ClearCache(provider);
    }

    public void ClearCache(TTSProvider provider)
    {
        switch (provider)
        {
            case TTSProvider.NTTS:
                NTTSHandler.Cache.Clear();
                break;

            case TTSProvider.Silero:
                SileroTTSHandler.Cache.Clear();
                break;
        }
    }

    // Masks NetManagerMethod for handling client setting
    private void SendTtsMessage(EntityEventArgs message, ICommonSession recipient)
    {
        if (_sessionsNotToSend.Contains(recipient))
            return;

        if (recipient.Status == SessionStatus.Disconnected)
            return;

        RaiseNetworkEvent(message, recipient);
    }

    private static string GenerateCacheKey(string text, TTSProvider? provider = null, string? speaker = null, TtsKind? kind = null)
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

    private sealed class TTSCache()
    {
        private readonly ConcurrentDictionary<string, TTSResponse> _lookup = new();
        private readonly ConcurrentQueue<string> _keysQueue = new();

        public int Limit
        {
            get => _limit;
            set => _limit = Math.Max(value, 0);
        }

        private int _limit = 1;

        public TTSCache(int limit) : this()
        {
            Limit = limit;
        }

        public void Cache(string key, TTSResponse value)
        {
            var currentCount = _lookup.Count;
            while (currentCount > 0 && currentCount + 1 > Limit)
            {
                if (_keysQueue.TryDequeue(out var firstKey) && _lookup.TryRemove(firstKey, out var reuseBuffer))
                    reuseBuffer.GetHandle().Dispose();

                currentCount = _lookup.Count;
            }

            if (Limit != 0)
            {
                value.GetHandle();
                _lookup[key] = value;
                _keysQueue.Enqueue(key);
            }
        }

        public bool TryGet(string key, [NotNullWhen(true)] out TTSResponse? responce)
        {
            if (Limit == 0)
            {
                responce = null;
                return false;
            }

            return _lookup.TryGetValue(key, out responce);
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
                    reuseBuffer.GetHandle().Dispose();
            }

        }
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
}

[Virtual]
public class ReferenceCounter<T>(T value)
{
    public T Value = value;
    public int ReferenceCount => _referenceCount;

    private int _referenceCount = 0;

    public Handle GetHandle()
    {
        _referenceCount++;
        return new(this);
    }

    protected virtual void OnHandleDisposed()
    {
        _referenceCount--;
    }

    public struct Handle(ReferenceCounter<T> counter) : IDisposable
    {
        private readonly ReferenceCounter<T> _counter = counter;
        private bool _isValid = true;

        public void Dispose()
        {
            if (!_isValid) return;
            _isValid = false;
            _counter.OnHandleDisposed();
        }

        public readonly Handle GetHandle()
        {
            return _counter.GetHandle();
        }

        public readonly bool TryGetValue([NotNullWhen(true)] out T value)
        {
            value = _counter.Value;
            return _isValid;
        }
    }
}
public static class ReferenceCounterExtensions
{
    public static bool TryGetValue<T>(this ReferenceCounter<T>.Handle? handle, [NotNullWhen(true)] out T? value)
    {
        value = default;
        return handle.HasValue && handle.Value.TryGetValue(out value);
    }
}
