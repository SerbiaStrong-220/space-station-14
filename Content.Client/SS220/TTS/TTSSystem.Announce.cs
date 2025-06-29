// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.TTS;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Client.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    internal float VolumeAnnounce = 0f;
    internal EntityUid AnnouncementUid = EntityUid.Invalid;

    private void InitializeAnnounces()
    {
        _cfg.OnValueChanged(CCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged, true);
        _ttsManager.PlayAnnounceTtsReceived += OnAnnounceTtsPlay;
    }

    private void ShutdownAnnounces()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSAnnounceVolume, OnTtsAnnounceVolumeChanged);
        _ttsManager.PlayAnnounceTtsReceived -= OnAnnounceTtsPlay;
    }

    private void OnAnnounceTtsPlay(MsgPlayAnnounceTts msg)
    {
        // Early creation of entities can lead to crashes, so we postpone it as much as possible
        if (AnnouncementUid == EntityUid.Invalid)
            AnnouncementUid = Spawn(null);

        var volume = AdjustVolume(TtsKind.Announce);

        var audioParams = AudioParams.Default.WithVolume(volume);

        // Play announcement sound
        var announcementSoundPath = new ResPath(msg.AnnouncementSound);
        PlaySoundQueued(AnnouncementUid, announcementSoundPath, audioParams, true);

        // Play announcement itself
        PlayTtsBytes(msg.Data, AnnouncementUid, audioParams, true);
    }

    private void OnTtsAnnounceVolumeChanged(float volume)
    {
        VolumeAnnounce = volume;
    }
}
