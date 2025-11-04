// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.IO;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Commands;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.SS220.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    [Dependency] private readonly IAudioManager _audioManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// Reducing the volume of the TTS when whispering. Will be converted to logarithm.
    /// </summary>
    private const float WhisperFade = 4f;

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;

    private int _maxQueuedPerEntity = 20;
    private int _maxEntitiesQueued = 30;
    private readonly Dictionary<EntityUid, Queue<PlayRequest>> _playQueues = new();
    private readonly Dictionary<EntityUid, EntityUid?> _playingStreams = new();

    private readonly EntityUid _fakeRecipient = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");

        Subs.CVar(_cfg, CCVars220.MaxQueuedPerEntity, (x) => _maxQueuedPerEntity = x, true);
        Subs.CVar(_cfg, CCVars220.MaxEntitiesQueued, (x) => _maxEntitiesQueued = x, true);
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);

        SubscribeNetworkEvent<TtsQueueResetMessage>(OnQueueResetRequest);

        _ttsManager.PlayTtsReceived += OnPlayTts;

        InitializeAnnounces();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);

        _ttsManager.PlayTtsReceived -= OnPlayTts;

        ShutdownAnnounces();
        ResetQueuesAndEndStreams();
    }

    public void RequestGlobalTTS(string text, string voiceId)
    {
        RaiseNetworkEvent(new RequestGlobalTTSEvent(text, voiceId));
    }

    private void OnTtsVolumeChanged(float volume)
    {
        _volume = volume;
    }

    private void OnTtsRadioVolumeChanged(float volume)
    {
        _radioVolume = volume;
    }

    private void OnQueueResetRequest(TtsQueueResetMessage ev)
    {
        ResetQueuesAndEndStreams();
        _sawmill.Debug("TTS queue was cleared by request from the server.");
    }

    public void ResetQueuesAndEndStreams()
    {
        foreach (var key in _playingStreams.Keys)
        {
            _playingStreams[key] = _audio.Stop(_playingStreams[key]);
        }

        _playingStreams.Clear();
        _playQueues.Clear();
    }

    // Process sound queues on frame update
    public override void FrameUpdate(float frameTime)
    {
        var streamsToRemove = new List<EntityUid>();

        foreach (var (uid, stream) in _playingStreams)
        {
            if (!TryComp(stream, out AudioComponent? _))
            {
                streamsToRemove.Add(uid);
            }
        }

        foreach (var uid in streamsToRemove)
        {
            _playingStreams.Remove(uid);
        }

        var queueUidsToRemove = new List<EntityUid>();

        foreach (var (uid, queue) in _playQueues)
        {
            if (_playingStreams.ContainsKey(uid))
                continue;

            if (!queue.TryDequeue(out var request))
                continue;

            if (queue.Count == 0)
                queueUidsToRemove.Add(uid);

            AudioStream? audioStream;
            (EntityUid Entity, AudioComponent Component)? stream;
            switch (request)
            {
                case PlayRequestByAudioStream playRequestByAudio:
                    audioStream = playRequestByAudio.AudioStream;

                    if (request.PlayGlobal)
                        stream = _audio.PlayGlobal(audioStream, null, request.Params);
                    else
                        stream = _audio.PlayEntity(audioStream, uid, null, request.Params);
                    break;

                case PlayRequestBySoundSpecifier playRequestBySoundSpecifier:
                    if (request.PlayGlobal)
                        stream = _audio.PlayGlobal(playRequestBySoundSpecifier.Sound, Filter.Local(), false);
                    else
                        stream = _audio.PlayEntity(playRequestBySoundSpecifier.Sound, _fakeRecipient, uid);
                    break;

                default:
                    continue;
            }

            if (stream.HasValue && stream.Value.Component is not null)
            {
                _playingStreams.Add(uid, stream.Value.Entity);
            }
        }

        foreach (var queueUid in queueUidsToRemove)
        {
            _playQueues.Remove(queueUid);
        }
    }

    public void TryQueueRequest(EntityUid entity, PlayRequest request)
    {
        if (!_playQueues.TryGetValue(entity, out var queue))
        {
            if (_playQueues.Count >= _maxEntitiesQueued)
                return;

            queue = new();
            _playQueues.Add(entity, queue);
        }

        if (queue.Count >= _maxQueuedPerEntity)
            return;

        queue.Enqueue(request);
    }

    public void TryQueuePlayByAudioStream(EntityUid entity, AudioStream audioStream, AudioParams audioParams, bool globally = false)
    {
        var request = new PlayRequestByAudioStream(audioStream, audioParams, globally);
        TryQueueRequest(entity, request);
    }

    private void PlaySoundQueued(EntityUid entity, SoundSpecifier sound, bool globally = false)
    {
        var request = new PlayRequestBySoundSpecifier(sound, globally);
        TryQueueRequest(entity, request);
    }

    private void PlayTtsBytes(TtsAudioData data, EntityUid? sourceUid = null, AudioParams? audioParams = null, bool globally = false)
    {
        // _sawmill.Debug($"Play TTS audio {data.Length} bytes from {sourceUid} entity"); // hide console tts flood, noinfo for players
        if (data.Length == 0)
            return;

        var finalParams = audioParams ?? AudioParams.Default;

        using MemoryStream stream = new(data.Buffer);
        var audioStream = _audioManager.LoadAudioOggVorbis(stream);

        if (sourceUid == null)
        {
            _audio.PlayGlobal(audioStream, null);
        }
        else
        {
            if (sourceUid.HasValue && sourceUid.Value.IsValid())
                TryQueuePlayByAudioStream(sourceUid.Value, audioStream, finalParams, globally);
        }
    }

    private void OnPlayTts(MsgPlayTts msg)
    {
        var volume = AdjustVolume(msg.Kind);
        var audioParams = AudioParams.Default.WithVolume(volume);

        PlayTtsBytes(msg.Data, GetEntity(msg.SourceUid), audioParams);
    }

    private float AdjustVolume(TtsKind kind)
    {
        var volume = kind switch
        {
            TtsKind.Radio => _radioVolume,
            TtsKind.Announce => VolumeAnnounce,
            _ => _volume,
        };

        volume = SharedAudioSystem.GainToVolume(volume);

        if (kind == TtsKind.Whisper)
        {
            volume -= SharedAudioSystem.GainToVolume(WhisperFade);
        }

        return volume;
    }

    // Play requests //
    public abstract class PlayRequest
    {
        public readonly AudioParams Params = AudioParams.Default;
        public readonly bool PlayGlobal = false;

        public PlayRequest(AudioParams? audioParams = null, bool playGlobal = false)
        {
            PlayGlobal = playGlobal;
            if (audioParams.HasValue)
                Params = audioParams.Value;
        }
    }

    public sealed class PlayRequestByAudioStream : PlayRequest
    {
        public readonly AudioStream AudioStream;

        public PlayRequestByAudioStream(AudioStream audioStream, AudioParams? audioParams = null, bool playGlobal = false) : base (audioParams, playGlobal)
        {
            AudioStream = audioStream;
        }
    }

    public sealed class PlayRequestBySoundSpecifier : PlayRequest
    {
        public readonly SoundSpecifier Sound;

        public PlayRequestBySoundSpecifier(SoundSpecifier sound, bool playGlobal = false) : base(sound.Params, playGlobal)
        {
            Sound = sound;
        }
    }
}
