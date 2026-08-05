// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.IO;
using System.Linq;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
public sealed partial class TtsSystem : SharedTtsSystem
{
    [Dependency] private IAudioManager _audioManager = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    /// <summary>
    /// Reducing the volume of the TTS when whispering.
    /// </summary>
    private const float WhisperFade = 4f;

    private float _volume;
    private float _radioVolume;
    private float _announcementVolume;

    private int _queueSizeLimit = 20;
    private int _queuesCountLimit = 30;

    private readonly Dictionary<TtsCacheKey, Queue<PlayRequest>> _playQueues = [];
    private readonly Dictionary<TtsCacheKey, EntityUid?> _playingStreams = [];

    private readonly HashSet<TtsCacheKey> _queuesToRemoveBuffer = [];

    public override void Initialize()
    {
        base.Initialize();

        // remove if Robust PR for clientCVar subs merged
        _cfg.OnValueChanged(CCVars220.RecieveTTS, x => RaiseNetworkEvent(new ReceiveTtsCVarChanged(x)), true);
        //end

        Subs.CVar(_cfg, CCVars220.TtsPlayQueueSizeLimit, x => _queueSizeLimit = x, true);
        Subs.CVar(_cfg, CCVars220.TtsPlayQueuesCountLimit, x => _queuesCountLimit = x, true);
        Subs.CVar(_cfg, CCVars220.TTSVolume, x => _volume = x, true);
        Subs.CVar(_cfg, CCVars220.TTSRadioVolume, x => _radioVolume = x, true);
        Subs.CVar(_cfg, CCVars220.TTSAnnounceVolume, x => _announcementVolume = x, true);

        SubscribeNetworkEvent<TtsClearAllQueuesMessage>(OnClearAllQueues);
        SubscribeNetworkEvent<PlayTtsMessage>(OnPlayTtsMessage);

        InitializeCacheKeyGeneration();
    }

    // Process sound queues on frame update
    public override void FrameUpdate(float frameTime)
    {
        foreach (var (key, stream) in _playingStreams.Where(p => !HasComp<AudioComponent>(p.Value)))
            _playingStreams.Remove(key);

        foreach (var (key, queue) in _playQueues)
        {
            if (_playingStreams.ContainsKey(key))
                continue;

            var stream = PlayNextRequest(queue);
            if (stream == null)
            {
                _queuesToRemoveBuffer.Add(key);
                continue;
            }

            _playingStreams.Add(key, stream.Value);
        }

        foreach (var key in _queuesToRemoveBuffer)
            _playQueues.Remove(key);

        Entity<AudioComponent>? PlayNextRequest(Queue<PlayRequest> queue, bool recursive = true)
        {
            if (!queue.TryDequeue(out var request))
                return null;

            Entity<AudioComponent>? stream;
            switch (request)
            {
                case PlayRequestByAudioStream byAudio:
                    if (request.Meta.Source != null)
                        stream = _audio.PlayEntity(byAudio.AudioStream, GetEntity(request.Meta.Source.Value), null, request.Params);
                    else
                        stream = _audio.PlayGlobal(byAudio.AudioStream, null, request.Params);
                    break;

                case PlayRequestBySoundSpecifier bySoundSpecifier:
                    if (request.Meta.Source != null)
                        stream = _audio.PlayEntity(bySoundSpecifier.Sound, Filter.Local(), GetEntity(request.Meta.Source.Value), false);
                    else
                        stream = _audio.PlayGlobal(bySoundSpecifier.Sound, Filter.Local(), false);
                    break;

                default:
                    stream = null;
                    break;
            }

            if (stream == null && recursive)
                return PlayNextRequest(queue, recursive);

            return stream;
        }
    }

    private void OnClearAllQueues(TtsClearAllQueuesMessage ev)
    {
        ClearAllQueuesAndStreams();
        Log.Debug("TTS queues was cleared by server request");
    }

    private void OnPlayTtsMessage(PlayTtsMessage args)
    {
        foreach (var data in args.Datas)
        {
            var volume = GetVolume(data.TtsMetadata.Kind);
            var audioParams = AudioParams.Default.WithVolume(volume);

            QueuePlayTts(data.TtsData, data.TtsMetadata, audioParams);
        }
    }

    public void RequestVoiceTest(ProtoId<TtsVoicePrototype> voiceId)
    {
        RaiseNetworkEvent(new RequestTTSVoiceTestEvent(voiceId));
    }

    public void ClearAllQueuesAndStreams()
    {
        foreach (var stream in _playingStreams.Values)
            _audio.Stop(stream);

        _playingStreams.Clear();
        _playQueues.Clear();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ClearAllQueuesAndStreams();
    }

    private void QueuePlayTts(ITtsData ttsData, TtsMetadata meta, AudioParams? audioParams = null)
    {
        audioParams ??= AudioParams.Default;
        switch (ttsData)
        {
            case TtsAudioBufferData audioBuffer:
                if (audioBuffer.Length == 0)
                    break;

                using (var memoryStream = new MemoryStream(audioBuffer.Buffer))
                {
                    var audioStream = _audioManager.LoadAudioOggVorbis(memoryStream);
                    TryQueueRequest(new PlayRequestByAudioStream(audioStream, meta, audioParams.Value));
                }
                break;

            case TtsSoundSpecifierData soundSpecifier:
                TryQueueRequest(new PlayRequestBySoundSpecifier(soundSpecifier.SoundSpecifier, meta, audioParams.Value));
                break;
        }
    }

    private bool TryQueueRequest(PlayRequest request)
    {
        var cacheKey = GenerateCacheKey(request.Meta);

        if (!_playQueues.TryGetValue(cacheKey, out var queue))
        {
            if (_playQueues.Count >= _queuesCountLimit)
                return false;

            queue = new();
            _playQueues.Add(cacheKey, queue);
        }

        if (queue.Count >= _queueSizeLimit)
            return false;

        queue.Enqueue(request);
        return true;
    }

    private float GetVolume(TtsKind kind)
    {
        var volume = kind switch
        {
            TtsKind.Radio => _radioVolume,
            TtsKind.Announce => _announcementVolume,
            TtsKind.Whisper => _volume / WhisperFade,
            _ => _volume,
        };

        volume = SharedAudioSystem.GainToVolume(volume);
        return volume;
    }

    private abstract class PlayRequest(TtsMetadata meta, AudioParams audioParams)
    {
        public readonly TtsMetadata Meta = meta;
        public readonly AudioParams Params = audioParams;
    }

    private sealed class PlayRequestByAudioStream(AudioStream audioStream, TtsMetadata meta, AudioParams audioParams) : PlayRequest(meta, audioParams)
    {
        public readonly AudioStream AudioStream = audioStream;
    }

    private sealed class PlayRequestBySoundSpecifier(SoundSpecifier sound, TtsMetadata meta, AudioParams audioParams) : PlayRequest(meta, audioParams)
    {
        public readonly SoundSpecifier Sound = sound;
    }
}
