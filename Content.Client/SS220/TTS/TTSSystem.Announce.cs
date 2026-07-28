// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Robust.Shared.Audio;

namespace Content.Client.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    internal float VolumeAnnounce = 0f;
    internal EntityUid AnnouncementUid = EntityUid.FirstUid;

    private void InitializeAnnounces()
    {
        SubscribeNetworkEvent<PlayAnnounceTtsMessage>(OnPlayAnnounceMessage);

        _cfg.OnValueChanged(CCVars220.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
    }

    private void OnPlayAnnounceMessage(PlayAnnounceTtsMessage args)
    {
        // Early creation of entities can lead to crashes, so we postpone it as much as possible
        if (AnnouncementUid == EntityUid.Invalid)
            AnnouncementUid = Spawn(null);

        var volume = AdjustVolume(TtsKind.Announce);

        var audioParams = AudioParams.Default.WithVolume(volume);

        if ((args.PlayAudioMask & AudioWithTTSPlayOperation.PlayAudio) == AudioWithTTSPlayOperation.PlayAudio)
            PlaySoundQueued(AnnouncementUid, args.AnnouncementSound, new(TtsKind.Announce, ""), true);

        if ((args.PlayAudioMask & AudioWithTTSPlayOperation.PlayTTS) == AudioWithTTSPlayOperation.PlayTTS)
            QueuePlayTts(args.AudioData, new(TtsKind.Announce, ""), AnnouncementUid, audioParams, true);
    }

    private void ShutdownAnnounces()
    {
        _cfg.UnsubValueChanged(CCVars220.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
    }

    private void OnTtsAnnounceVolumeChanged(float volume)
    {
        VolumeAnnounce = volume;
    }
}
