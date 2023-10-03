using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.Corvax.TTS.Commands;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Plays TTS audio in world
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private ISawmill _sawmill = default!;

    private readonly MemoryContentRoot _contentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private float _volume = 0.0f;
    private float _radioVolume = 0.0f;
    private int _fileIdx = 0;

    private const int MaxQueuedPerEntity = 20;
    private readonly Dictionary<EntityUid, Queue<PlayRequest>> _playQueues = new();
    private readonly Dictionary<EntityUid, AudioSystem.PlayingStream> _playingStreams = new();

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _resourceCache.AddRoot(Prefix, _contentRoot);

        _cfg.OnValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged, true);
        _cfg.OnValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged, true);

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<TtsQueueResetMessage>(OnQueueResetRequest);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnTtsVolumeChanged);
        _cfg.UnsubValueChanged(CCCVars.TTSRadioVolume, OnTtsRadioVolumeChanged);
        _contentRoot.Dispose();
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
        foreach (var (_, stream) in _playingStreams)
        {
            stream.Stop();
        }

        _playingStreams.Clear();
        _playQueues.Clear();
    }

    // Process sound queues on frame update
    public override void FrameUpdate(float frameTime)
    {
        var streamsToRemove = new HashSet<EntityUid>();

        foreach (var (uid, stream) in _playingStreams)
        {
            if (stream.Done)
                streamsToRemove.Add(uid);
        }

        foreach (var uid in streamsToRemove)
        {
            _playingStreams.Remove(uid);
        }

        foreach (var (uid, queue) in _playQueues)
        {
            if (_playingStreams.ContainsKey(uid))
                continue;

            if (!queue.TryDequeue(out var request))
                continue;

            var filePath = new ResPath($"{request.FileIdx}.ogg");
            var soundPath = new SoundPathSpecifier(Prefix / filePath, request.Params);
            var stream = _audio.PlayEntity(soundPath, new EntityUid(), uid);
            if (stream is AudioSystem.PlayingStream playingStream)
            {
                _playingStreams.Add(uid, playingStream);
            }

            _contentRoot.RemoveFile(filePath);
        }
    }

    public void TryQueuePlay(EntityUid entity, int fileIdx, AudioParams audioParams)
    {
        var request = new PlayRequest(fileIdx, audioParams);

        if (!_playQueues.TryGetValue(entity, out var queue))
        {
            queue = new();
            _playQueues.Add(entity, queue);
        }

        if (queue.Count >= MaxQueuedPerEntity)
            return;

        queue.Enqueue(request);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        _sawmill.Debug($"Play TTS audio {ev.Data.Length} bytes from {ev.SourceUid} entity");

        var volume = (ev.IsRadio ? _radioVolume : _volume) * ev.VolumeModifier;

        var filePath = new ResPath($"{_fileIdx}.ogg");
        _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioParams = AudioParams.Default.WithVolume(volume);
        var soundPath = new SoundPathSpecifier(Prefix / filePath, audioParams);
        if (ev.SourceUid == null)
        {
            _audio.PlayGlobal(soundPath, Filter.Local(), false);
            _contentRoot.RemoveFile(filePath);
        }
        else
        {
            var entity = GetEntity(ev.SourceUid);
            if (entity.HasValue && entity.Value.IsValid())
                TryQueuePlay(entity.Value, _fileIdx, audioParams);
        }

        _fileIdx++;
    }

    public sealed class PlayRequest
    {
        public readonly AudioParams Params = AudioParams.Default;
        public readonly int FileIdx = 0;

        public PlayRequest(int fileIdx, AudioParams? audioParams = null)
        {
            FileIdx = fileIdx;
            if (audioParams.HasValue)
                Params = audioParams.Value;
        }
    }
}
